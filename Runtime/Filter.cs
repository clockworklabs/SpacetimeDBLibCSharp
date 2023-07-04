namespace SpacetimeDB.Filter;

using SpacetimeDB.SATS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;

class ErasedValue
{
    private static readonly TypeInfo<ErasedValue> erasedTypeInfo = new TypeInfo<ErasedValue>(
        // uninhabited type (sum type with zero variants)
        // we don't really intent to use it but need to put something here to conform to the GetSatsTypeInfo() "interface"
        new SumType(),
        (reader) => throw new NotSupportedException("cannot deserialize type-erased value"),
        (writer, value) => value.write(writer)
    );

    public static TypeInfo<ErasedValue> GetSatsTypeInfo() => erasedTypeInfo;

    private readonly Action<BinaryWriter> write;

    public ErasedValue(Action<BinaryWriter> write)
    {
        this.write = write;
    }
}

[SpacetimeDB.Type]
partial struct Rhs : SpacetimeDB.TaggedEnum<(ErasedValue Value, byte Field)> { }

[SpacetimeDB.Type]
partial struct CmpArgs
{
    public byte LhsField;
    public Rhs Rhs;

    public CmpArgs(byte lhsField, Rhs rhs)
    {
        LhsField = lhsField;
        Rhs = rhs;
    }
}

[SpacetimeDB.Type]
enum OpCmp
{
    Eq,
    NotEq,
    Lt,
    LtEq,
    Gt,
    GtEq,
}

[SpacetimeDB.Type]
partial struct Cmp
{
    public OpCmp op;
    public CmpArgs args;

    public Cmp(OpCmp op, CmpArgs args)
    {
        this.op = op;
        this.args = args;
    }
}

enum OpLogic
{
    And,
    Or,
}

[SpacetimeDB.Type]
partial struct Logic
{
    public Expr lhs;

    public OpLogic op;
    public Expr rhs;

    public Logic(Expr lhs, OpLogic op, Expr rhs)
    {
        this.lhs = lhs;
        this.op = op;
        this.rhs = rhs;
    }
}

enum OpUnary
{
    Not,
}

[SpacetimeDB.Type]
partial struct Unary
{
    public OpUnary op;
    public Expr arg;

    public Unary(OpUnary op, Expr arg)
    {
        this.op = op;
        this.arg = arg;
    }
}

[SpacetimeDB.Type]
partial struct Expr : SpacetimeDB.TaggedEnum<(Cmp Cmp, Logic Logic, Unary Unary)> { }

public class Filter
{
    private readonly KeyValuePair<string, TypeInfo<object?>>[] fieldTypeInfos;

    private Filter(KeyValuePair<string, TypeInfo<object?>>[] fieldTypeInfos)
    {
        this.fieldTypeInfos = fieldTypeInfos;
    }

    public static byte[] Compile<T>(
        KeyValuePair<string, TypeInfo<object?>>[] fieldTypeInfos,
        Expression<Func<T, bool>> rowFilter
    )
    {
        var filter = new Filter(fieldTypeInfos);
        var expr = filter.HandleExpr(rowFilter.Body);
        var bytes = Expr.GetSatsTypeInfo().ToBytes(expr);
        return bytes;
    }

    byte ExprAsTableField(Expression expr) =>
        expr switch
        {
            MemberExpression { Expression: ParameterExpression, Member: { Name: var memberName } }
                => (byte)Array.FindIndex(fieldTypeInfos, pair => pair.Key == memberName),
            _
                => throw new NotSupportedException(
                    "expected table field access in the left-hand side of a comparison"
                )
        };

    object? ExprAsConstant(Expression expr) =>
        expr switch
        {
            ConstantExpression { Value: var value } => value,
            _
                => throw new NotSupportedException(
                    "expected constant expression in the right-hand side of a comparison"
                )
        };

    Cmp HandleCmp(BinaryExpression expr)
    {
        var lhsFieldIndex = ExprAsTableField(expr.Left);

        // TODO: implement handling of conversions for non-int integer types (byte, short, etc.)
        // I gave it a try, but ran into some bizarre crashes, so leaving for later.
        var rhs = ExprAsConstant(expr.Right);
        var rhsWrite = fieldTypeInfos[lhsFieldIndex].Value.Write;
        var erasedRhs = new ErasedValue((writer) => rhsWrite(writer, rhs));

        var args = new CmpArgs(lhsFieldIndex, new Rhs { Value = erasedRhs });

        var op = expr.NodeType switch
        {
            ExpressionType.Equal => OpCmp.Eq,
            ExpressionType.NotEqual => OpCmp.NotEq,
            ExpressionType.LessThan => OpCmp.Lt,
            ExpressionType.LessThanOrEqual => OpCmp.LtEq,
            ExpressionType.GreaterThan => OpCmp.Gt,
            ExpressionType.GreaterThanOrEqual => OpCmp.GtEq,
            _ => throw new NotSupportedException("unsupported comparison operation")
        };

        return new Cmp(op, args);
    }

    Logic HandleLogic(BinaryExpression expr)
    {
        var lhs = HandleExpr(expr.Left);
        var rhs = HandleExpr(expr.Right);

        var op = expr.NodeType switch
        {
            ExpressionType.And => OpLogic.And,
            ExpressionType.Or => OpLogic.Or,
            _ => throw new NotSupportedException("unsupported logic operation")
        };

        return new Logic(lhs, op, rhs);
    }

    Expr HandleBinary(BinaryExpression expr) =>
        expr switch
        {
            BinaryExpression
            {
                NodeType: ExpressionType.Equal
                    or ExpressionType.NotEqual
                    or ExpressionType.LessThan
                    or ExpressionType.LessThanOrEqual
                    or ExpressionType.GreaterThan
                    or ExpressionType.GreaterThanOrEqual
            }
                => new Expr { Cmp = HandleCmp(expr) },
            BinaryExpression { NodeType: ExpressionType.And or ExpressionType.Or }
                => new Expr { Logic = HandleLogic(expr) },
            _ => throw new NotSupportedException("unsupported expression")
        };

    Expr HandleUnary(UnaryExpression expr)
    {
        var arg = HandleExpr(expr.Operand);

        var op = expr.NodeType switch
        {
            ExpressionType.Not => OpUnary.Not,
            _ => throw new NotSupportedException("unsupported unary operation")
        };

        return new Expr { Unary = new Unary(op, arg) };
    }

    Expr HandleExpr(Expression expr) =>
        expr switch
        {
            BinaryExpression binExpr => HandleBinary(binExpr),
            UnaryExpression unExpr => HandleUnary(unExpr),
            _ => throw new NotSupportedException("unsupported expression")
        };
}
