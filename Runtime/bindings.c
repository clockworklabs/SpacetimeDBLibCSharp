#include <stdint.h>

// ABI version

// __attribute__((export_name("SPACETIME_ABI_VERSION"))) -
// doesn't work on non-functions, must specify on command line
const uint32_t SPACETIME_ABI_VERSION = /* 3.0 */ (3 << 16) | 0;
const uint8_t SPACETIME_ABI_VERSION_IS_ADDR = 1;

// Shims to avoid dependency on WASI in the generated Wasm file.

// Based on
// https://github.com/WebAssembly/wasi-libc/blob/main/libc-bottom-half/sources/__wasilibc_real.c,

int32_t __imported_wasi_snapshot_preview1_args_get(int32_t arg0, int32_t arg1) {
  return 0;
}

int32_t __imported_wasi_snapshot_preview1_args_sizes_get(int32_t arg0,
                                                         int32_t arg1) {
  return 0;
}

int32_t __imported_wasi_snapshot_preview1_environ_get(int32_t arg0,
                                                      int32_t arg1) {
  return 0;
}

int32_t __imported_wasi_snapshot_preview1_environ_sizes_get(int32_t arg0,
                                                            int32_t arg1) {
  return 0;
}

int32_t __imported_wasi_snapshot_preview1_clock_res_get(int32_t arg0,
                                                        uint64_t* timestamp) {
  *timestamp = 1;
  return 0;
}

int32_t __imported_wasi_snapshot_preview1_clock_time_get(int32_t arg0,
                                                         int64_t arg1,
                                                         int32_t arg2) {
  return 0;
}

int32_t __imported_wasi_snapshot_preview1_fd_advise(int32_t arg0,
                                                    int64_t arg1,
                                                    int64_t arg2,
                                                    int32_t arg3) {
  return 0;
}

int32_t __imported_wasi_snapshot_preview1_fd_write(int32_t arg0,
                                                   int32_t arg1,
                                                   int32_t arg2,
                                                   int32_t arg3) {
  return 0;
}

int32_t __imported_wasi_snapshot_preview1_fd_allocate(int32_t arg0,
                                                      int64_t arg1,
                                                      int64_t arg2) {
  return 0;
}

int32_t __imported_wasi_snapshot_preview1_fd_close(int32_t arg0) {
  return 0;
}

int32_t __imported_wasi_snapshot_preview1_fd_datasync(int32_t arg0) {
  return 0;
}

int32_t __imported_wasi_snapshot_preview1_fd_fdstat_get(int32_t arg0,
                                                        int32_t arg1) {
  return 0;
}

int32_t __imported_wasi_snapshot_preview1_fd_fdstat_set_flags(int32_t arg0,
                                                              int32_t arg1) {
  return 0;
}

int32_t __imported_wasi_snapshot_preview1_fd_fdstat_set_rights(int32_t arg0,
                                                               int64_t arg1,
                                                               int64_t arg2) {
  return 0;
}

int32_t __imported_wasi_snapshot_preview1_fd_filestat_get(int32_t arg0,
                                                          int32_t arg1) {
  return 0;
}

int32_t __imported_wasi_snapshot_preview1_fd_filestat_set_size(int32_t arg0,
                                                               int64_t arg1) {
  return 0;
}

int32_t __imported_wasi_snapshot_preview1_fd_filestat_set_times(int32_t arg0,
                                                                int64_t arg1,
                                                                int64_t arg2,
                                                                int32_t arg3) {
  return 0;
}

int32_t __imported_wasi_snapshot_preview1_fd_pread(int32_t arg0,
                                                   int32_t arg1,
                                                   int32_t arg2,
                                                   int64_t arg3,
                                                   int32_t arg4) {
  return 0;
}

int32_t __imported_wasi_snapshot_preview1_fd_prestat_get(int32_t arg0,
                                                         int32_t arg1) {
  // Return this value to indicate there are no further preopens to iterate
  // through
  return /* __WASI_ERRNO_BADF */ 8;
}

int32_t __imported_wasi_snapshot_preview1_fd_prestat_dir_name(int32_t arg0,
                                                              int32_t arg1,
                                                              int32_t arg2) {
  return 0;
}

int32_t __imported_wasi_snapshot_preview1_fd_pwrite(int32_t arg0,
                                                    int32_t arg1,
                                                    int32_t arg2,
                                                    int64_t arg3,
                                                    int32_t arg4) {
  return 0;
}

int32_t __imported_wasi_snapshot_preview1_fd_read(int32_t arg0,
                                                  int32_t arg1,
                                                  int32_t arg2,
                                                  int32_t arg3) {
  return 0;
}

int32_t __imported_wasi_snapshot_preview1_fd_readdir(int32_t arg0,
                                                     int32_t arg1,
                                                     int32_t arg2,
                                                     int64_t arg3,
                                                     int32_t arg4) {
  return 0;
}

int32_t __imported_wasi_snapshot_preview1_fd_renumber(int32_t arg0,
                                                      int32_t arg1) {
  return 0;
}

int32_t __imported_wasi_snapshot_preview1_fd_seek(int32_t arg0,
                                                  int64_t arg1,
                                                  int32_t arg2,
                                                  int32_t arg3) {
  return 0;
}

int32_t __imported_wasi_snapshot_preview1_fd_sync(int32_t arg0) {
  return 0;
}

int32_t __imported_wasi_snapshot_preview1_fd_tell(int32_t arg0, int32_t arg1) {
  return 0;
}

int32_t __imported_wasi_snapshot_preview1_path_create_directory(int32_t arg0,
                                                                int32_t arg1,
                                                                int32_t arg2) {
  return 0;
}

int32_t __imported_wasi_snapshot_preview1_path_filestat_get(int32_t arg0,
                                                            int32_t arg1,
                                                            int32_t arg2,
                                                            int32_t arg3,
                                                            int32_t arg4) {
  return 0;
}

int32_t __imported_wasi_snapshot_preview1_path_filestat_set_times(
    int32_t arg0,
    int32_t arg1,
    int32_t arg2,
    int32_t arg3,
    int64_t arg4,
    int64_t arg5,
    int32_t arg6) {
  return 0;
}

int32_t __imported_wasi_snapshot_preview1_path_link(int32_t arg0,
                                                    int32_t arg1,
                                                    int32_t arg2,
                                                    int32_t arg3,
                                                    int32_t arg4,
                                                    int32_t arg5,
                                                    int32_t arg6) {
  return 0;
}

int32_t __imported_wasi_snapshot_preview1_path_open(int32_t arg0,
                                                    int32_t arg1,
                                                    int32_t arg2,
                                                    int32_t arg3,
                                                    int32_t arg4,
                                                    int64_t arg5,
                                                    int64_t arg6,
                                                    int32_t arg7,
                                                    int32_t arg8) {
  return 0;
}

int32_t __imported_wasi_snapshot_preview1_path_readlink(int32_t arg0,
                                                        int32_t arg1,
                                                        int32_t arg2,
                                                        int32_t arg3,
                                                        int32_t arg4,
                                                        int32_t arg5) {
  return 0;
}

int32_t __imported_wasi_snapshot_preview1_path_remove_directory(int32_t arg0,
                                                                int32_t arg1,
                                                                int32_t arg2) {
  return 0;
}

int32_t __imported_wasi_snapshot_preview1_path_rename(int32_t arg0,
                                                      int32_t arg1,
                                                      int32_t arg2,
                                                      int32_t arg3,
                                                      int32_t arg4,
                                                      int32_t arg5) {
  return 0;
}

int32_t __imported_wasi_snapshot_preview1_path_symlink(int32_t arg0,
                                                       int32_t arg1,
                                                       int32_t arg2,
                                                       int32_t arg3,
                                                       int32_t arg4) {
  return 0;
}

int32_t __imported_wasi_snapshot_preview1_path_unlink_file(int32_t arg0,
                                                           int32_t arg1,
                                                           int32_t arg2) {
  return 0;
}

int32_t __imported_wasi_snapshot_preview1_poll_oneoff(int32_t arg0,
                                                      int32_t arg1,
                                                      int32_t arg2,
                                                      int32_t arg3) {
  return 0;
}

_Noreturn void __imported_wasi_snapshot_preview1_proc_exit(int32_t arg0) {
  __builtin_trap();
}

int32_t __imported_wasi_snapshot_preview1_sched_yield() {
  return 0;
}

int32_t __imported_wasi_snapshot_preview1_random_get(int32_t arg0,
                                                     int32_t arg1) {
  return 0;
}

int32_t __imported_wasi_snapshot_preview1_sock_accept(int32_t arg0,
                                                      int32_t arg1,
                                                      int32_t arg2) {
  return 0;
}

int32_t __imported_wasi_snapshot_preview1_sock_recv(int32_t arg0,
                                                    int32_t arg1,
                                                    int32_t arg2,
                                                    int32_t arg3,
                                                    int32_t arg4,
                                                    int32_t arg5) {
  return 0;
}

int32_t __imported_wasi_snapshot_preview1_sock_send(int32_t arg0,
                                                    int32_t arg1,
                                                    int32_t arg2,
                                                    int32_t arg3,
                                                    int32_t arg4) {
  return 0;
}

int32_t __imported_wasi_snapshot_preview1_sock_shutdown(int32_t arg0,
                                                        int32_t arg1) {
  return 0;
}

#ifdef _REENTRANT
int32_t __imported_wasi_thread_spawn(int32_t arg0) {
  return 0;
}
#endif
