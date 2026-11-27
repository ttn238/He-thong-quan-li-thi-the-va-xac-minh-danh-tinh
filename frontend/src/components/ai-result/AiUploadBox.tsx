import { useEffect, useRef, useState } from 'react';
import { aiResultService } from '../../services/aiResultService';
import { S } from './aiResultStyles';

const ACCEPT = ['image/jpeg', 'image/png', 'image/webp', 'image/bmp'];
const MAX_BYTES = 10 * 1024 * 1024;

export default function AiUploadBox({ onUploaded }: { onUploaded: (requestId: string) => void }) {
    const [file, setFile] = useState<File | null>(null);
    const [preview, setPreview] = useState<string | null>(null);
    const [linked, setLinked] = useState('');
    const [busy, setBusy] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [dragging, setDragging] = useState(false);
    const input = useRef<HTMLInputElement>(null);

    useEffect(() => () => { if (preview) URL.revokeObjectURL(preview); }, [preview]);

    const choose = (next?: File) => {
        if (!next) return;
        if (!ACCEPT.includes(next.type)) return setError('Chỉ hỗ trợ ảnh JPG, PNG, WEBP hoặc BMP.');
        if (!next.size) return setError('Tệp ảnh đang rỗng.');
        if (next.size > MAX_BYTES) return setError('Ảnh vượt quá 10 MB.');
        setError(null);
        setFile(next);
        setPreview(URL.createObjectURL(next));
    };

    const submit = async () => {
        if (!file) return setError('Hãy chọn ảnh cần phân tích.');
        setBusy(true);
        setError(null);
        try {
            const result = await aiResultService.analyze(file, linked);
            onUploaded(result.requestId);
        } catch (e) {
            setError(e instanceof Error ? e.message : 'Không gửi được ảnh.');
            setBusy(false);
        }
    };

    return (
        <section className={S.card}>
            <header className={S.head}><h3 className={S.title}>Tải ảnh hiện trường để AI phân tích</h3></header>
            <div
                onDragOver={(e) => { e.preventDefault(); setDragging(true); }}
                onDragLeave={() => setDragging(false)}
                onDrop={(e) => { e.preventDefault(); setDragging(false); choose(e.dataTransfer.files[0]); }}
                className={`rounded-md border-2 border-dashed p-6 text-center ${dragging ? 'border-brand bg-blue-50' : 'border-line bg-soft'}`}
            >
                {preview
                    ? <img src={preview} alt="Ảnh đã chọn" className="mx-auto mb-3 max-h-64 rounded" />
                    : <p className="m-0 mb-3 text-sm text-muted">Kéo thả ảnh vào đây hoặc chọn tệp bên dưới.</p>}
                <p className={S.muted}>{file ? `${file.name} · ${(file.size / 1048576).toFixed(1)} MB` : 'JPG, PNG, WEBP, BMP · tối đa 10 MB'}</p>
                <button type="button" className={S.btnGhost} onClick={() => input.current?.click()}>Chọn hình ảnh</button>
                <input ref={input} type="file" accept={ACCEPT.join(',')} className="hidden"
                    onChange={(e) => { choose(e.target.files?.[0]); e.target.value = ''; }} />
            </div>
            <label className="mt-3 block text-sm">
                <span className="text-muted">Mã tin báo liên kết (nếu có)</span>
                <input value={linked} onChange={(e) => setLinked(e.target.value)} maxLength={120}
                    placeholder="Ví dụ: TB-20261014-0412" className="mt-1 block w-full rounded border border-line px-2 py-1.5 text-sm" />
            </label>
            {error && <p role="alert" className="my-2 text-[13px] text-red-700">{error}</p>}
            <div className="mt-3">
                <button className={S.btn} disabled={busy || !file} onClick={() => void submit()}>
                    {busy ? 'Đang phân tích…' : 'Gửi phân tích'}
                </button>
            </div>
        </section>
    );
}
