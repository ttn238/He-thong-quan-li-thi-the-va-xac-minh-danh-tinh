export interface User {
  maTaiKhoan: number;
  tenDangNhap: string;
  hoTen: string;
  email?: string;
  soDienThoai?: string;
  cccd?: string;
  donVi?: string;
  chucVu?: string;
  maVaiTro: number;
  tenVaiTro: string;
  trangThai: 'HoatDong' | 'Khoa' | 'KhoaTam';
  soLanDangNhapSai: number;
  khoaDenThoiDiem?: string;
  lanDangNhapCuoi?: string;
  ngayTao: string;
  ngayCapNhat?: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageIndex: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface UserFilterParams {
  keyword?: string;
  maVaiTro?: number;
  trangThai?: string;
  donVi?: string;
  pageIndex?: number;
  pageSize?: number;
}

export interface CreateUserPayload {
  tenDangNhap: string;
  matKhau: string;
  hoTen: string;
  email?: string;
  soDienThoai?: string;
  cccd?: string;
  donVi?: string;
  chucVu?: string;
  maVaiTro: number;
}

export interface UpdateUserPayload {
  hoTen: string;
  email?: string;
  soDienThoai?: string;
  cccd?: string;
  donVi?: string;
  chucVu?: string;
  maVaiTro: number;
}

export interface UpdateProfilePayload {
  hoTen: string;
  email?: string;
  soDienThoai?: string;
}

export interface ChangeStatusPayload {
  trangThai: 'HoatDong' | 'Khoa' | 'KhoaTam';
}

export interface ResetPasswordPayload {
  matKhauMoi: string;
}
