export type MissingPersonStatus = 'DangTimKiem' | 'DaXacDinh' | 'DaDong';

export interface MissingPersonRelative {
  maNguoiThan: number;
  maHoSoMatTich: number;
  hoTen: string;
  quanHe?: string | null;
  soDienThoai?: string | null;
  diaChi?: string | null;
}

export interface MissingPersonImage {
  maHinhAnh: number;
  tenGoc: string;
  duongDan: string;
  loaiHinhAnh: string;
  dinhDang: string;
  kichThuoc: number;
  kichThuocHienThi?: string;
  laNhayCam: boolean;
  moTa?: string | null;
  thoiGianTaiLen: string;
  nguoiTaiLen: string;
}

export interface MissingPersonMatch {
  maDoiSanh: number;
  maHoSoThiThe: number;
  maHoSoMatTich: number;
  hoTenNguoiMatTich: string;
  diemDoiSanh?: number | null;
  diemHienThi?: string;
  mucDoPhuHop?: string | null;
  nhanMucDoPhuHop?: string;
  trangThai: string;
  nhanTrangThai?: string;
  tieuChi?: string | null;
  thoiGianYeuCau: string;
  nguoiYeuCau?: string | null;
}

export interface MissingPersonDetail {
  maHoSoMatTich: number;
  hoTen: string;
  ngaySinh?: string | null;
  tuoiUocTinh?: number | null;
  gioiTinh?: string | null;
  nhanGioiTinh?: string;
  thoiGianMatTich?: string | null;
  noiMatTich?: string | null;
  trangThai: MissingPersonStatus | string;
  nhanTrangThai?: string;
  mauTrangThai?: string;
  mauChuTrangThai?: string;
  nguoiThan: MissingPersonRelative[];
  hinhAnh: MissingPersonImage[];
  ketQuaDoiSanh: MissingPersonMatch[];
  coQuyenXemHinhAnh: boolean;
  coQuyenXemDoiSanh: boolean;
}

export const MISSING_STATUS_LABELS: Record<string, string> = {
  DangTimKiem: 'Đang tìm kiếm',
  DaXacDinh: 'Đã xác định',
  DaDong: 'Đã đóng',
};

export const MISSING_STATUS_BADGES: Record<string, string> = {
  DangTimKiem: 'bg-amber-100 text-amber-800 border-amber-200',
  DaXacDinh: 'bg-emerald-100 text-emerald-800 border-emerald-200',
  DaDong: 'bg-slate-200 text-slate-700 border-slate-300',
};
