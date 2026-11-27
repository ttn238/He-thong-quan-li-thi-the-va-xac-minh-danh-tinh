import { useEffect, useRef, useState } from 'react';
import { bodyService } from '../../services/bodyService';
import {
    LOAI_ANH_HO_SO_THI_THE,
    MAX_ANH_BYTES,
    MIME_ANH_CHO_PHEP,
    NHAN_LOAI_ANH,
    type BodyImageItem,
    type LoaiAnhHoSoThiThe,
} from '../../types/Body';

const CHAP_NHAN = MIME_ANH_CHO_PHEP.join(',');

type LoiTruyen = { loi: string | null };

/** Kiểm tra tệp ở máy khách trước khi gửi; backend vẫn kiểm tra lại nội dung thật. */
function kiemTraTep(file: File | undefined): LoiTruyen {
    if (!file) return { loi: 'Hãy chọn tệp ảnh.' };
    if (!MIME_ANH_CHO_PHEP.includes(file.type as (typeof MIME_ANH_CHO_PHEP)[number]))
        return { loi: 'Chỉ hỗ trợ ảnh JPG, PNG, WEBP hoặc BMP.' };
    if (!file.size) return { loi: 'Tệp ảnh đang rỗng.' };
    if (file.size > MAX_ANH_BYTES) return { loi: 'Ảnh vượt quá 10 MB.' };
    return { loi: null };
}

export interface BodyImageUploaderProps {
    maHoSoThiThe: number;
    onUploaded: (anh: BodyImageItem) => void;
}

export const BodyImageUploader: React.FC<BodyImageUploaderProps> = ({ maHoSoThiThe, onUploaded }) => {
    const [file, setFile] = useState<File | null>(null);
    const [preview, setPreview] = useState<string | null>(null);
    const [loai, setLoai] = useState<LoaiAnhHoSoThiThe>('ThiThe_KhamNghiem');
    const [moTa, setMoTa] = useState('');
    const [busy, setBusy] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [dragging, setDragging] = useState(false);
    const input = useRef<HTMLInputElement>(null);

    useEffect(() => () => { if (preview) URL.revokeObjectURL(preview); }, [preview]);

    const chonTep = (next?: File) => {
        const ketQua = kiemTraTep(next);
        setError(ketQua.loi);
        if (ketQua.loi) return;
        setFile(next!);
        setPreview(URL.createObjectURL(next!));
    };

    const gui = async () => {
        const kiemLai = kiemTraTep(file ?? undefined);
        if (kiemLai.loi) return setError(kiemLai.loi);

        setBusy(true);
        setError(null);
        try {
            const uploaded = await bodyService.uploadImage(maHoSoThiThe, file!, loai, moTa);
            onUploaded(uploaded);
            setFile(null);
            setMoTa('');
            setPreview(null);
        } catch (e) {
            // Giữ nguyên ảnh đã chọn để người dùng thử lại mà không phải chọn lại tệp
            setError(e instanceof Error ? e.message : 'Không tải được ảnh lên máy chủ.');
        } finally {
            setBusy(false);
        }
    };

    return (
        <section className="rounded-lg border border-slate-200 bg-white p-4">
            <h3 className="mb-3 text-sm font-semibold text-slate-800">Tải ảnh lên hồ sơ</h3>

            <div
                onDragOver={(e) => { e.preventDefault(); setDragging(true); }}
                onDragLeave={() => setDragging(false)}
                onDrop={(e) => { e.preventDefault(); setDragging(false); chonTep(e.dataTransfer.files[0]); }}
                className={`rounded-md border-2 border-dashed p-6 text-center ${dragging ? 'border-brand bg-blue-50' : 'border-line bg-soft'}`}
            >
                {preview
                    ? <img src={preview} alt="Ảnh đã chọn" className="mx-auto mb-3 max-h-64 rounded" />
                    : <p className="m-0 mb-3 text-sm text-muted">Kéo thả ảnh vào đây hoặc chọn tệp bên dưới.</p>}
                <p className="m-0 text-xs text-muted">
                    {file ? `${file.name} · ${(file.size / 1048576).toFixed(1)} MB` : 'JPG, PNG, WEBP, BMP · tối đa 10 MB'}
                </p>
                <button
                    type="button"
                    className="mt-3 rounded border border-line bg-white px-3 py-1.5 text-sm hover:bg-slate-50 disabled:opacity-50"
                    onClick={() => input.current?.click()}
                    disabled={busy}
                >
                    Chọn hình ảnh
                </button>
                <input
                    ref={input}
                    type="file"
                    accept={CHAP_NHAN}
                    className="hidden"
                    onChange={(e) => { chonTep(e.target.files?.[0]); e.target.value = ''; }}
                />
            </div>

            <div className="mt-3 grid gap-3 sm:grid-cols-2">
                <label className="block text-sm">
                    <span className="text-muted">Loại ảnh</span>
                    <select
                        value={loai}
                        onChange={(e) => setLoai(e.target.value as LoaiAnhHoSoThiThe)}
                        disabled={busy}
                        className="mt-1 block w-full rounded border border-line px-2 py-1.5 text-sm"
                    >
                        {LOAI_ANH_HO_SO_THI_THE.map((x) => (
                            <option key={x} value={x}>{NHAN_LOAI_ANH[x]}</option>
                        ))}
                    </select>
                </label>
                <label className="block text-sm">
                    <span className="text-muted">Mô tả (tuỳ chọn)</span>
                    <input
                        value={moTa}
                        onChange={(e) => setMoTa(e.target.value)}
                        maxLength={500}
                        disabled={busy}
                        placeholder="Ví dụ: vết sẹo cẳng tay trái"
                        className="mt-1 block w-full rounded border border-line px-2 py-1.5 text-sm"
                    />
                </label>
            </div>

            <p className="mt-2 text-xs text-muted">
                Ảnh gắn hồ sơ thi thể được coi là dữ liệu nhạy cảm: hệ thống lưu vào vùng riêng và chỉ cán bộ có quyền xem được.
            </p>

            {error && <p role="alert" className="mt-2 rounded bg-red-50 px-3 py-2 text-[13px] text-red-700">{error}</p>}

            <div className="mt-3">
                <button
                    type="button"
                    className="rounded bg-brand px-4 py-2 text-sm font-medium text-white hover:opacity-90 disabled:opacity-50"
                    disabled={busy || !file}
                    onClick={() => void gui()}
                >
                    {busy ? 'Đang tải lên…' : 'Tải ảnh lên'}
                </button>
            </div>
        </section>
    );
};

export default BodyImageUploader;
