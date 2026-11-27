import { useEffect, useState } from 'react';
import { bodyService, dingDangKichThuocAnh } from '../../services/bodyService';
import { NHAN_LOAI_ANH, type BodyImageItem } from '../../types/Body';

/**
 * Một ảnh thu nhỏ chỉ hiển thị được sau khi lấy byte qua endpoint có JWT.
 * Không có URL công khai cho ảnh hồ sơ thi thể (NFR1.2).
 */
const AnhThuNho: React.FC<{ maHoSoThiThe: number; anh: BodyImageItem }> = ({ maHoSoThiThe, anh }) => {
    const [src, setSrc] = useState<string | null>(null);
    const [dangTai, setDangTai] = useState(true);
    const [loi, setLoi] = useState(false);

    useEffect(() => {
        let active = true;
        let objectUrl: string | undefined;
        setSrc(null);
        setLoi(false);
        setDangTai(true);

        bodyService.getImage(maHoSoThiThe, anh.maHinhAnh)
            .then((blob) => {
                objectUrl = URL.createObjectURL(blob);
                if (!active) return void URL.revokeObjectURL(objectUrl);
                setSrc(objectUrl);
                setDangTai(false);
            })
            .catch(() => {
                if (!active) return;
                setLoi(true);
                setDangTai(false);
            });

        return () => {
            active = false;
            if (objectUrl) URL.revokeObjectURL(objectUrl);
        };
    }, [maHoSoThiThe, anh.maHinhAnh]);

    return (
        <figure className="m-0 overflow-hidden rounded-lg border border-slate-200 bg-white">
            <div className="flex aspect-square items-center justify-center bg-soft">
                {dangTai && <span className="text-xs text-muted">Đang tải ảnh…</span>}
                {!dangTai && loi && (
                    <span className="px-3 text-center text-xs text-red-700">
                        Không tải được ảnh. Có thể bạn không còn quyền xem ảnh này.
                    </span>
                )}
                {!dangTai && !loi && src && (
                    <img src={src} alt={anh.tenGoc} className="h-full w-full object-cover" />
                )}
            </div>
            <figcaption className="space-y-1 border-t border-slate-100 p-3">
                <p className="truncate text-[13px] font-medium text-slate-800" title={anh.tenGoc}>{anh.tenGoc}</p>
                <div className="flex flex-wrap gap-1 text-[11px]">
                    <span className="rounded bg-slate-100 px-1.5 py-0.5 text-slate-700">
                        {NHAN_LOAI_ANH[anh.loaiHinhAnh as keyof typeof NHAN_LOAI_ANH] ?? anh.loaiHinhAnh}
                    </span>
                    {anh.laNhayCam && <span className="rounded bg-amber-100 px-1.5 py-0.5 text-amber-800">Nhạy cảm</span>}
                </div>
                <p className="m-0 text-[11px] text-muted">
                    {anh.kichThuocHienThi ?? dingDangKichThuocAnh(anh.kichThuoc)} · {anh.nguoiTaiLen} ·{' '}
                    {new Date(anh.thoiGianTaiLen).toLocaleString('vi-VN')}
                </p>
                {anh.moTa && <p className="m-0 text-[11px] text-slate-600">{anh.moTa}</p>}
            </figcaption>
        </figure>
    );
};

export interface BodyImageGalleryProps {
    maHoSoThiThe: number;
    danhSach: BodyImageItem[];
}

export const BodyImageGallery: React.FC<BodyImageGalleryProps> = ({ maHoSoThiThe, danhSach }) => {
    if (!danhSach.length) {
        return (
            <p className="rounded-md border border-dashed border-line bg-soft px-4 py-6 text-center text-sm text-muted">
                Hồ sơ này chưa có ảnh nào.
            </p>
        );
    }

    return (
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4">
            {danhSach.map((anh) => (
                <AnhThuNho key={anh.maHinhAnh} maHoSoThiThe={maHoSoThiThe} anh={anh} />
            ))}
        </div>
    );
};

export default BodyImageGallery;
