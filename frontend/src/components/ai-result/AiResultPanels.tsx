import { useEffect, useState } from 'react';
import type { AiResult, AiResultImage, BBox, DetectedObject, MissingField, OcrItem, SuggestedLocation } from '../../types/AiResult';
import { aiResultService } from '../../services/aiResultService';
import { LOW_CONFIDENCE, bboxText, dt, pct, text } from './aiResultUtils';

const copy = (value: string) => { void navigator.clipboard?.writeText(value); };
const KIND_LABEL = { street_sign: 'Biển tên đường', license_plate: 'Biển số xe', other: 'Văn bản khác' } as const;
const coords = (b: BBox): [number, number, number, number] => [b.xMin, b.yMin, b.xMax - b.xMin, b.yMax - b.yMin];

export function MetaPanel({ data }: { data: AiResult }) {
    return <section className="ai-card">
        <header className="ai-card__head"><h3>Thông tin yêu cầu phân tích</h3></header>
        <dl className="ai-kv">
            <dt>Mã yêu cầu AI</dt><dd>#{text(data.requestId)}</dd>
            <dt>Hồ sơ liên kết</dt><dd>{data.linkedRecordId ? `#${data.linkedRecordId}` : 'Chưa liên kết hồ sơ'}</dd>
            <dt>Thời điểm gửi</dt><dd>{dt(data.submittedAt)}</dd>
            <dt>Hoàn tất suy luận</dt><dd>{dt(data.completedAt)}{data.durationMs ? ` (${(data.durationMs / 1000).toFixed(2)} giây)` : ''}</dd>
            <dt>Động cơ xử lý</dt><dd>{text(data.engine)}</dd>
            <dt>Mã băm ảnh</dt><dd className="ai-mono">{text(data.imageHash)}</dd>
        </dl>
    </section>;
}

export function ImagePanel({ image, requestId, ocr, objects }: {
    image?: AiResultImage; requestId: string; ocr: OcrItem[]; objects: DetectedObject[];
}) {
    const [show, setShow] = useState(true);
    const [src, setSrc] = useState<string | null>(null);
    const [imageError, setImageError] = useState(false);
    const w = image?.width;
    const h = image?.height;
    useEffect(() => {
        let active = true;
        let objectUrl: string | undefined;
        setImageError(false);
        void aiResultService.getImage(requestId).then((blob) => {
            objectUrl = URL.createObjectURL(blob);
            if (active) setSrc(objectUrl);
            else URL.revokeObjectURL(objectUrl);
        }).catch(() => { if (active) setImageError(true); });
        return () => { active = false; if (objectUrl) URL.revokeObjectURL(objectUrl); };
    }, [requestId]);
    const boxes = [
        ...ocr.flatMap((o) => o.bbox ? [{ b: coords(o.bbox), label: o.text, cls: 'ocr' }] : []),
        ...objects.map((o) => ({ b: coords(o.box), label: o.label, cls: 'obj' })),
    ];
    const canDraw = !!w && !!h && boxes.length > 0;
    return <section className="ai-card">
        <header className="ai-card__head">
            <h3>Hình ảnh đầu vào</h3>
            {canDraw && <button className="ai-link" onClick={() => setShow((value) => !value)}>{show ? 'Ẩn' : 'Hiện'} khung nhận diện</button>}
        </header>
        <div className="ai-image" style={{ aspectRatio: w && h ? `${w} / ${h}` : '16 / 9' }}>
            {src ? <img src={src} alt={text(image?.fileName, 'Ảnh phân tích')} />
                : <div className="ai-image__empty">{imageError ? 'Ảnh không tải được.' : 'Đang tải ảnh xem trước…'}</div>}
            {show && canDraw && boxes.map((item, i) => (
                <div key={i} className={`ai-box ai-box--${item.cls}`}
                    style={{ left: `${item.b[0] / w! * 100}%`, top: `${item.b[1] / h! * 100}%`, width: `${item.b[2] / w! * 100}%`, height: `${item.b[3] / h! * 100}%` }}>
                    <span>{item.label}</span>
                </div>
            ))}
        </div>
        <p className="ai-muted">{text(image?.fileName)} · {image?.sizeBytes ? `${Math.round(image.sizeBytes / 1024)} KB` : 'Chưa rõ dung lượng'} · {text(image?.format)}</p>
    </section>;
}

export function OcrPanel({ items }: { items: OcrItem[] }) {
    return <section className="ai-card">
        <header className="ai-card__head"><h3>1. Văn bản trích xuất (OCR)</h3>
            {items.length > 0 && <button className="ai-link" onClick={() => copy(items.map((item) => item.text).join('\n'))}>Sao chép toàn bộ</button>}
        </header>
        {items.length === 0 ? <p className="ai-muted">Không nhận dạng được văn bản nào trong ảnh.</p> : <ul className="ai-list">
            {items.map((item, i) => <li key={i} className={item.confidence < LOW_CONFIDENCE ? 'ai-item ai-item--low' : 'ai-item'}>
                <div><strong>{text(item.text)}</strong> <span className="ai-tag">{KIND_LABEL[item.kind ?? 'other']}</span>
                    {item.confidence < LOW_CONFIDENCE && <span className="ai-tag ai-tag--warn">Cần thẩm tra</span>}</div>
                <div className="ai-muted">Độ tin cậy: {pct(item.confidence)} · Khung: {bboxText(item.bbox)}{item.note ? ` · ${item.note}` : ''}</div>
                <button className="ai-link" onClick={() => copy(item.text)}>Sao chép</button>
            </li>)}
        </ul>}
    </section>;
}

export function ObjectsPanel({ items }: { items: DetectedObject[] }) {
    return <section className="ai-card">
        <header className="ai-card__head"><h3>2. Đối tượng được phát hiện</h3><span className="ai-tag">{items.length} đối tượng</span></header>
        {items.length === 0 ? <p className="ai-muted">Không phát hiện đối tượng thuộc các lớp hỗ trợ.</p> : <div className="ai-grid2">
            {items.map((item, i) => <article key={i} className={item.confidence < LOW_CONFIDENCE ? 'ai-item ai-item--low' : 'ai-item'}>
                <div><strong>{text(item.label)}</strong> <span className="ai-tag">{pct(item.confidence)}</span></div>
                <p>Nhóm: {item.category === 'person' ? 'Người' : 'Phương tiện'}</p>
                <div className="ai-muted">Khung: {bboxText(item.box)}</div>
                {Object.keys(item.attributes ?? {}).length > 0 && <ul className="ai-muted">
                    {Object.entries(item.attributes).map(([key, value]) => <li key={key}>{key}: {value}</li>)}
                </ul>}
            </article>)}
        </div>}
    </section>;
}

export function LocationsPanel({ items }: { items: SuggestedLocation[] }) {
    const sorted = [...items].sort((a, b) => a.rank - b.rank);
    return <section className="ai-card">
        <header className="ai-card__head"><h3>3. Địa điểm suy luận</h3><span className="ai-tag ai-tag--warn">Gợi ý AI, chưa xác minh</span></header>
        {sorted.length === 0 ? <p className="ai-muted">AI chưa gợi ý địa điểm. Cán bộ cần xác minh vị trí thực địa.</p> : <ul className="ai-list">
            {sorted.map((item) => <li key={item.rank} className="ai-item">
                <div><span className="ai-tag">Ưu tiên {item.rank}</span> <strong>{text(item.name)}</strong> <span className="ai-tag">{pct(item.confidence)}</span></div>
                <div className="ai-muted">{text(item.adminArea, 'Chưa rõ địa bàn')} · Tọa độ: {item.lat !== undefined && item.lng !== undefined ? `${item.lat}, ${item.lng}` : 'Chưa có'}</div>
                {item.basis && <div className="ai-muted">Cơ sở suy luận: {item.basis}</div>}
            </li>)}
        </ul>}
    </section>;
}

const FIELD_STATUS: Record<MissingField['status'], string> = {
    done: 'Hoàn tất', missing: 'Thiếu dữ liệu', out_of_scope: 'Ngoài phạm vi', unsupported: 'Chưa hỗ trợ',
};

export function PartialTable({ fields }: { fields: MissingField[] }) {
    return <section className="ai-card ai-card--warn">
        <header className="ai-card__head"><h3>Thông tin cần kiểm tra thêm</h3></header>
        <div className="ai-scroll"><table className="ai-table">
            <thead><tr><th>Hạng mục</th><th>Trạng thái</th><th>Giá trị</th><th>Khuyến nghị</th></tr></thead>
            <tbody>{fields.map((field, i) => <tr key={i}>
                <td>{text(field.field)}</td><td>{FIELD_STATUS[field.status]}</td>
                <td>{text(field.value, '—')}</td><td>{text(field.recommendation, '—')}</td>
            </tr>)}</tbody>
        </table></div>
    </section>;
}
