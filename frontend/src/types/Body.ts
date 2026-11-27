// Kiểu dữ liệu cho hồ sơ thi thể, khớp DTO backend (JSON camelCase).
// Nguồn: backend/src/DeathManagement.API/DTOs/Body/BodyDto.cs, BodyDetailDto.cs, DTOs/Image/ImageDto.cs

export type BodyStatus =
  | 'MoiTiepNhan'
  | 'DangLuuGiu'
  | 'DangXacMinh'
  | 'DaXacDinh'
  | 'DaBanGiao'
  | 'DongHoSo'
  | 'TiepTucLuuGiu';

export type BodyGender = 'Nam' | 'Nu' | 'Khac' | 'KhongXacDinh';

export type BodyCharacteristicType =
  | 'Seo'
  | 'HinhXam'
  | 'VetBot'
  | 'Rang'
  | 'DiTat'
  | 'TocDa'
  | 'TrangPhuc'
  | 'Khac';

export interface BodyCharacteristic {
  maDacDiem: number;
  maHoSoThiThe: number;
  loaiDacDiem: BodyCharacteristicType | string;
  nhanLoaiDacDiem?: string;
  viTriCoThe?: string | null;
  moTa: string;
  ngayGhiNhan: string;
}

export interface SaveBodyCharacteristicPayload {
  loaiDacDiem: BodyCharacteristicType;
  viTriCoThe?: string;
  moTa: string;
  ngayGhiNhan?: string;
}

export interface SaveBodyCharacteristicResponse {
  message: string;
  data: BodyCharacteristic;
}

/** Bản ghi hồ sơ thi thể dùng cho danh sách và phần đầu trang chi tiết. */
export interface BodyRecord {
  maHoSoThiThe: number;
  thoiGianPhatHien: string;
  diaDiem?: string | null;
  trangThai: BodyStatus | string;
  nhanTrangThai?: string;
  mauTrangThai?: string;
  mauChuTrangThai?: string;
  gioiTinh?: BodyGender | string | null;
  nhanGioiTinh?: string;
  tuoiUocLuong?: number | null;
  chieuCao?: number | null;
  dacDiemCoThe?: string | null;
  maTinBao?: number | null;
  maViTri?: number | null;
  khu?: string | null;
  tu?: string | null;
  ngan?: string | null;
  viTriLuuTruHienThi?: string;
  trangThaiTiepTheoHopLe?: string[];
  cheDoHanCheNhaXac?: boolean;
  dacDiemNhanDang?: BodyCharacteristic[];
}

/** Ảnh đính kèm hồ sơ thi thể (BodyImageItemDto). duongDan là đường dẫn nội bộ, không phải URL để hiển thị. */
export interface BodyImageItem {
  maHinhAnh: number;
  tenGoc: string;
  duongDan: string;
  loaiHinhAnh: string;
  dinhDang: string;
  kichThuoc: number;
  laNhayCam: boolean;
  moTa?: string | null;
  thoiGianTaiLen: string;
  nguoiTaiLen: string;
  kichThuocHienThi?: string;
  nhanNhayCam?: string;
}

/** Phản hồi của endpoint tải ảnh (ImageDto). */
export interface BodyImageUploadResult {
  maHinhAnh: number;
  tenFile: string;
  tenGoc: string;
  duongDan: string;
  loaiHinhAnh: string;
  dinhDang: string;
  kichThuoc: number;
  laNhayCam: boolean;
  moTa?: string | null;
  maHoSoThiThe?: number | null;
  maTaiKhoanTaiLen: number;
  nguoiTaiLen: string;
  thoiGianTaiLen: string;
  trangThai: string;
}

/** Chi tiết hồ sơ thi thể kèm danh sách ảnh và cờ quyền. */
export interface BodyDetail extends BodyRecord {
  hinhAnh?: BodyImageItem[];
  coQuyenXemHinhAnh?: boolean;
  cheDoHanCheNhaXac?: boolean;
  coQuyenXemDoVat?: boolean;
  coQuyenXemDoiSanh?: boolean;
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

export const BODY_CHARACTERISTIC_TYPE_OPTIONS: { value: BodyCharacteristicType; label: string }[] = [
  { value: 'Seo', label: 'Vết sẹo' },
  { value: 'HinhXam', label: 'Hình xăm' },
  { value: 'VetBot', label: 'Vết bớt' },
  { value: 'Rang', label: 'Đặc điểm răng' },
  { value: 'DiTat', label: 'Dị tật' },
  { value: 'TocDa', label: 'Tóc / Da' },
  { value: 'TrangPhuc', label: 'Trang phục' },
  { value: 'Khac', label: 'Đặc điểm khác' },
];

export const BODY_CHARACTERISTIC_TYPE_LABELS: Record<string, string> = {
  Seo: 'Vết sẹo',
  HinhXam: 'Hình xăm',
  VetBot: 'Vết bớt',
  Rang: 'Đặc điểm răng',
  DiTat: 'Dị tật',
  TocDa: 'Tóc / Da',
  TrangPhuc: 'Trang phục',
  Khac: 'Đặc điểm khác',
};

/** Loại ảnh hợp lệ cho hồ sơ thi thể (ImageHelper.TypeBody*). */
export const LOAI_ANH_HO_SO_THI_THE = [
  'ThiThe_KhamNghiem',
  'ThiThe_KhuonMat',
  'ThiThe_ToanThan',
] as const;

export type LoaiAnhHoSoThiThe = (typeof LOAI_ANH_HO_SO_THI_THE)[number];

export const NHAN_LOAI_ANH: Record<LoaiAnhHoSoThiThe, string> = {
  ThiThe_KhamNghiem: 'Ảnh khám nghiệm',
  ThiThe_KhuonMat: 'Ảnh khuôn mặt',
  ThiThe_ToanThan: 'Ảnh toàn thân',
};

/** Phải khớp FileHelper.MaxFileSizeInBytes và AllowedImageExtensions bên backend. */
export const MAX_ANH_BYTES = 10 * 1024 * 1024;
export const MIME_ANH_CHO_PHEP = ['image/jpeg', 'image/png', 'image/webp', 'image/bmp'] as const;
