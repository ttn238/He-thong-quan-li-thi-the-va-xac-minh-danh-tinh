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
  maCodeVaiTro?: string;
  trangThai: 'HoatDong' | 'Khoa' | 'KhoaTam';
  soLanDangNhapSai: number;
  khoaDenThoiDiem?: string;
  lanDangNhapCuoi?: string;
  ngayTao: string;
  ngayCapNhat?: string;
}

export interface RoleDefinition {
  id: number;
  code: string;
  label: string;
  badgeColor: string;
}

export const SYSTEM_ROLES: RoleDefinition[] = [
  { id: 1, code: 'ADMIN', label: 'Quản trị hệ thống', badgeColor: 'bg-red-100 text-red-800 border-red-200' },
  { id: 2, code: 'CONG_AN', label: 'Công an / Cán bộ xác minh', badgeColor: 'bg-blue-100 text-blue-800 border-blue-200' },
  { id: 3, code: 'CONG_AN_DUYET', label: 'Công an có thẩm quyền duyệt', badgeColor: 'bg-purple-100 text-purple-800 border-purple-200' },
  { id: 4, code: 'NHA_XAC', label: 'Nhân viên nhà xác', badgeColor: 'bg-gray-100 text-gray-800 border-gray-200' },
  { id: 5, code: 'NGUOI_DAN', label: 'Người dân / Thân nhân', badgeColor: 'bg-green-100 text-green-800 border-green-200' },
];

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageIndex: number;
  pageSize: number;
  totalPages?: number;
  hasPreviousPage?: boolean;
  hasNextPage?: boolean;
}

export interface UserFilterParams {
  keyword?: string;
  maVaiTro?: number;
  trangThai?: string;
  donVi?: string;
  pageIndex?: number;
  pageSize?: number;
}

export interface CreateUserRequestPayload {
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

export type CreateUserPayload = CreateUserRequestPayload;

export interface UpdateUserRequestPayload {
  hoTen: string;
  email?: string;
  soDienThoai?: string;
  cccd?: string;
  donVi?: string;
  chucVu?: string;
  maVaiTro: number;
}

export type UpdateUserPayload = UpdateUserRequestPayload;

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
