import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import BodyImageGallery from '../../components/upload/BodyImageGallery';
import BodyImageUploader from '../../components/upload/BodyImageUploader';
import { authHelper } from '../../features/auth/authHelper';
import { bodyService } from '../../services/bodyService';
import type { BodyDetail, BodyImageItem } from '../../types/Body';

const THE = ({ label, value }: { label: string; value?: string | number | null }) => (
    <div>
        <dt className="text-xs text-muted">{label}</dt>
        <dd className="m-0 text-sm text-slate-800">{value ?? '—'}</dd>
    </div>
);

export const BodyProfile: React.FC = () => {
    const { id } = useParams<{ id: string }>();
    const maHoSo = Number(id);
    const [detail, setDetail] = useState<BodyDetail | null>(null);
    const [danhSachAnh, setDanhSachAnh] = useState<BodyImageItem[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [thongBao, setThongBao] = useState<string | null>(null);

    // Cán bộ điều tra và nhà xác có FILE:CREATE; người dân thì bị chặn vì ảnh thi thể là dữ liệu nhạy cảm
    const duocTaiAnh = authHelper.isOfficer() || authHelper.isApprover() || authHelper.isMorgue();

    useEffect(() => {
        if (!Number.isInteger(maHoSo) || maHoSo <= 0) {
            setError('Mã hồ sơ thi thể không hợp lệ.');
            setLoading(false);
            return;
        }

        let active = true;
        setLoading(true);
        setError(null);

        bodyService.getDetail(maHoSo)
            .then((d) => {
                if (!active) return;
                setDetail(d);
                setDanhSachAnh(d.hinhAnh ?? []);
            })
            .catch((e: unknown) => {
                if (active) setError(e instanceof Error ? e.message : 'Không tải được hồ sơ.');
            })
            .finally(() => {
                if (active) setLoading(false);
            });

        return () => { active = false; };
    }, [maHoSo]);

    const onUploaded = (anh: BodyImageItem) => {
        setDanhSachAnh((cu) => [anh, ...cu.filter((x) => x.maHinhAnh !== anh.maHinhAnh)]);
        setThongBao(`Đã tải ảnh "${anh.tenGoc}" lên hồ sơ #${maHoSo}.`);
    };

    if (loading) return <p className="p-6 text-sm text-muted">Đang tải hồ sơ thi thể…</p>;

    if (error) {
        return (
            <div className="mx-auto max-w-3xl p-6">
                <div role="alert" className="rounded border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
                    {error}
                </div>
            </div>
        );
    }

    if (!detail) return null;

    return (
        <div className="mx-auto max-w-6xl space-y-4 p-4 sm:p-6">
            <header className="flex flex-wrap items-center justify-between gap-3">
                <div>
                    <h1 className="m-0 text-lg font-semibold text-slate-900">Hồ sơ thi thể #{detail.maHoSoThiThe}</h1>
                    <span
                        className="mt-1 inline-block rounded px-2 py-0.5 text-xs font-medium"
                        style={{ backgroundColor: detail.mauTrangThai, color: detail.mauChuTrangThai }}
                    >
                        {detail.nhanTrangThai}
                    </span>
                </div>
            </header>

            <section className="rounded-lg border border-slate-200 bg-white p-4">
                <dl className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4">
                    <THE label="Thời gian phát hiện" value={new Date(detail.thoiGianPhatHien).toLocaleString('vi-VN')} />
                    <THE label="Địa điểm" value={detail.diaDiem} />
                    <THE label="Giới tính" value={detail.nhanGioiTinh} />
                    <THE label="Tuổi ước lượng" value={detail.tuoiUocLuong} />
                    <THE label="Chiều cao (cm)" value={detail.chieuCao} />
                    <THE label="Vị trí lưu trữ" value={detail.viTriLuuTruHienThi} />
                    <THE label="Đặc điểm cơ thể" value={detail.dacDiemCoThe} />
                </dl>
            </section>

            <section className="space-y-3">
                <h2 className="text-base font-semibold text-slate-900">Hình ảnh hồ sơ</h2>

                {!detail.coQuyenXemHinhAnh ? (
                    <p className="rounded-md border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800">
                        Tài khoản của bạn không có quyền xem hình ảnh của hồ sơ thi thể.
                    </p>
                ) : (
                    <div className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_320px]">
                        <BodyImageGallery maHoSoThiThe={detail.maHoSoThiThe} danhSach={danhSachAnh} />
                        {duocTaiAnh && <BodyImageUploader maHoSoThiThe={detail.maHoSoThiThe} onUploaded={onUploaded} />}
                    </div>
                )}

                {thongBao && <p className="text-sm text-green-700">{thongBao}</p>}
            </section>
        </div>
    );
};

export default BodyProfile;
