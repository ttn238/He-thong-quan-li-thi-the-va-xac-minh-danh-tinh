export type BodyStatus =
  | 'MoiTiepNhan'
  | 'DangLuuGiu'
  | 'DangXacMinh'
  | 'DaXacDinh'
  | 'DaBanGiao'
  | 'DongHoSo'
  | 'TiepTucLuuGiu';

export type BodyGender = 'Nam' | 'Nu' | 'Khac' | 'KhongXacDinh';

export interface BodyRecord {
  maHoSoThiThe: number;
  thoiGianPhatHien: string;
  diaDiem?: string | null;
  trangThai: BodyStatus | string;
  gioiTinh?: BodyGender | string | null;
  tuoiUocLuong?: number | null;
  chieuCao?: number | null;
  dacDiemCoThe?: string | null;
  maTinBao?: number | null;
  maViTri?: number | null;
  khu?: string | null;
  tu?: string | null;
  ngan?: string | null;
  nhanTrangThai?: string;
  nhanGioiTinh?: string;
}

export interface BodyFilterParams {
  tuKhoa?: string;
  trangThai?: string;
  gioiTinh?: string;
  tuNgay?: string;
  denNgay?: string;
  trang?: number;
  soLuong?: number;
}

export interface BodyListResponse {
  items: BodyRecord[];
  total: number;
  trang: number;
  soLuong: number;
}

/**
 * Payload cập nhật hồ sơ thi thể (PUT /bodies/{id}).
 * Không bao gồm các trường cố định/hạn chế như maHoSoThiThe, trangThai, maTinBao, khuonMat.
 */
export interface UpdateBodyPayload {
  thoiGianPhatHien?: string;
  diaDiem?: string;
  gioiTinh?: BodyGender;
  tuoiUocLuong?: number;
  chieuCao?: number;
  dacDiemCoThe?: string;
}

export interface UpdateBodyResponse {
  message: string;
  data?: BodyRecord;
}

export const BODY_STATUS_LABELS: Record<string, string> = {
  MoiTiepNhan: 'Mới tiếp nhận',
  DangLuuGiu: 'Đang lưu giữ',
  DangXacMinh: 'Đang xác minh',
  DaXacDinh: 'Đã xác định',
  DaBanGiao: 'Đã bàn giao',
  DongHoSo: 'Đóng hồ sơ',
  TiepTucLuuGiu: 'Tiếp tục lưu giữ',
};

export const BODY_STATUS_BADGES: Record<string, string> = {
  MoiTiepNhan: 'bg-sky-100 text-sky-800 border-sky-200',
  DangLuuGiu: 'bg-indigo-100 text-indigo-800 border-indigo-200',
  DangXacMinh: 'bg-amber-100 text-amber-800 border-amber-200',
  DaXacDinh: 'bg-emerald-100 text-emerald-800 border-emerald-200',
  DaBanGiao: 'bg-purple-100 text-purple-800 border-purple-200',
  DongHoSo: 'bg-slate-200 text-slate-700 border-slate-300',
  TiepTucLuuGiu: 'bg-orange-100 text-orange-800 border-orange-200',
};

export const BODY_GENDER_OPTIONS: { value: BodyGender; label: string }[] = [
  { value: 'Nam', label: 'Nam' },
  { value: 'Nu', label: 'Nữ' },
  { value: 'Khac', label: 'Khác' },
  { value: 'KhongXacDinh', label: 'Không xác định' },
];

export const BODY_GENDER_LABELS: Record<string, string> = {
  Nam: 'Nam',
  Nu: 'Nữ',
  Khac: 'Khác',
  KhongXacDinh: 'Không xác định',
};
