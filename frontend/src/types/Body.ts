// Kiểu dữ liệu cho hồ sơ thi thể, khớp DTO backend (JSON camelCase).
// Nguồn: backend/src/DeathManagement.API/DTOs/Body/BodyDto.cs, BodyDetailDto.cs, DTOs/Image/ImageDto.cs

/** Bản ghi hồ sơ thi thể dùng cho danh sách và phần đầu trang chi tiết. */
export interface BodyRecord {
    maHoSoThiThe: number;
    thoiGianPhatHien: string;
    diaDiem?: string | null;
    trangThai: string;
    /** Nhãn trạng thái tiếng Việt do backend sinh để hiển thị nhất quán. */
    nhanTrangThai: string;
    mauTrangThai: string;
    mauChuTrangThai: string;
    gioiTinh?: string | null;
    nhanGioiTinh?: string;
    tuoiUocLuong?: number | null;
    chieuCao?: number | null;
    dacDiemCoThe?: string | null;
    maTinBao?: number | null;
    khu?: string | null;
    tu?: string | null;
    ngan?: string | null;
    viTriLuuTruHienThi?: string;
    trangThaiTiepTheoHopLe?: string[];
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

/** Chi tiết hồ sơ thi thể. Các nhóm dữ liệu nhạy cảm khác không dùng ở màn tải ảnh nên để tùy chọn. */
export interface BodyDetail extends BodyRecord {
    hinhAnh?: BodyImageItem[];
    /** Backend quyết định theo quyền FILE:VIEW của người đang đăng nhập. */
    coQuyenXemHinhAnh?: boolean;
    cheDoHanCheNhaXac?: boolean;
    coQuyenXemDoVat?: boolean;
    coQuyenXemDoiSanh?: boolean;
}

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
