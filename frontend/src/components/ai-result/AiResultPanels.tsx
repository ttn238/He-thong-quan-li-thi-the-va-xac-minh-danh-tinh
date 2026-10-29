import { useState } from 'react';
import type {
    AiResult, AiResultImage, DetectedObject, MissingField, OcrItem, SuggestedLocation,
} from '../../types/AiResult';
import { LOW_CONFIDENCE, bboxText, dt, pct, text } from './aiResultUtils';

const copy = (t: string) => { void navigator.clipboard?.writeText(t); };

const KIND_LABEL = { street_sign: 'Biển tên đường', license_plate: 'Biển số xe', other: 'Văn bản khác' } as const;

export function MetaPanel({ data }: { data: AiResult }) {
    return (
        <section className= "ai-card" >
        <header className="ai-card__head" > <h3>Thông tin yêu cầu phân tích < /h3></header >
            <dl className="ai-kv" >
                <dt>Mã yêu cầu AI < /dt><dd>#{text(data.requestId)}</dd >
                    <dt>Hồ sơ liên kết < /dt><dd>{data.linkedRecordId ? `#${data.linkedRecordId}` : 'Chưa liên kết hồ sơ'}</dd >
                        <dt>Thời điểm gửi < /dt><dd>{dt(data.submittedAt)}</dd >
                            <dt>Hoàn tất suy luận </dt>
                                < dd > { dt(data.completedAt) }{ data.durationMs ? ` (${(data.durationMs / 1000).toFixed(2)}s)` : '' } </dd>
                                    < dt > Động cơ xử lý < /dt><dd>{text(data.engine)}</dd >
                                        <dt>Mã băm toàn vẹn < /dt><dd className="ai-mono">{text(data.imageHash)}</dd >
                                            </dl>
                                            </section>
  );
}

export function ImagePanel({ image, ocr, objects }: { image?: AiResultImage; ocr: OcrItem[]; objects: DetectedObject[] }) {
    const [show, setShow] = useState(true);
    const w = image?.width;
    const h = image?.height;
    const boxes = [
        ...ocr.flatMap((o) => (o.bbox ? [{ b: o.bbox, label: o.text, cls: 'ocr' }] : [])),
        ...objects.flatMap((o) => (o.bbox ? [{ b: o.bbox, label: o.label, cls: 'obj' }] : [])),
    ];
    const canDraw = !!w && !!h && boxes.length > 0;
    return (
        <section className= "ai-card" >
        <header className="ai-card__head" >
            <h3>Hình ảnh đầu vào </h3>
    { canDraw && <button className="ai-link" onClick = {() => setShow((s) => !s) }> { show? 'Ẩn': 'Hiện' } khung nhận diện </button>
}
</header>
    < div className = "ai-image" style = {{ aspectRatio: w && h ? `${w} / ${h}` : '16 / 9' }}>
    { image?.url?<img src = {image.url } alt = { text(image.fileName) } /> : <div className="ai-image__empty" > Chưa có ảnh xem trước </div>}
{
    show && canDraw && boxes.map((x, i) => (
        <div key= { i } className = {`ai-box ai-box--${x.cls}`}
style = {{ left: `${(x.b[0] / w!) * 100}%`, top: `${(x.b[1] / h!) * 100}%`, width: `${(x.b[2] / w!) * 100}%`, height: `${(x.b[3] / h!) * 100}%` }}>
    <span>{ x.label } </span>
    </div>
        ))}
</div>
    < p className = "ai-muted" >
    { text(image?.fileName) } · { image?.sizeBytes ? `${Math.round(image.sizeBytes / 1024)} KB` : 'Chưa rõ dung lượng' } · { text(image?.format) }
</p>
    </section>
  );
}

export function OcrPanel({ items }: { items: OcrItem[] }) {
    return (
        <section className= "ai-card" >
        <header className="ai-card__head" >
            <h3>1. Văn bản trích xuất(OCR) </h3>
    { items.length > 0 && <button className="ai-link" onClick = {() => copy(items.map((i) => i.text).join('\n')) }> Sao chép toàn bộ </button>
}
</header>
{
    items.length === 0 ? <p className="ai-muted" > Không nhận dạng được văn bản nào trong ảnh.</p> : (
        < ul className = "ai-list" >
        {
            items.map((it, i) => {
                const low = it.confidence < LOW_CONFIDENCE;
                return (
                    <li key= { i } className = { low? 'ai-item ai-item--low': 'ai-item' } >
                        <div>
                        <strong>{ text(it.text)
        } </strong>
    { it.kind && <span className="ai-tag" > { KIND_LABEL[it.kind]} </span> }
    { low && <span className="ai-tag ai-tag--warn" > Độ tin cậy thấp, cần thẩm tra </span> }
    </div>
        < div className = "ai-muted" >
            Độ tin cậy: { pct(it.confidence) } · BBox: { bboxText(it.bbox) } { it.note ? ` · ${it.note}` : '' }
    </div>
        < button className = "ai-link" onClick = {() => copy(it.text)
}> Sao chép </button>
    </li>
            );
          })}
</ul>
      )}
</section>
  );
}

export function ObjectsPanel({ items }: { items: DetectedObject[] }) {
    return (
        <section className= "ai-card" >
        <header className="ai-card__head" > <h3>2. Đối tượng & đặc điểm nhận diện < /h3><span className="ai-tag">{items.length} đối tượng</span > </header>
    {
        items.length === 0 ? <p className="ai-muted" > Không phát hiện đối tượng nào.</p> : (
            < div className = "ai-grid2" >
            {
                items.map((o, i) => (
                    <article key= { i } className = { o.confidence < LOW_CONFIDENCE ? 'ai-item ai-item--low' : 'ai-item' } >
                    <div><strong>{ text(o.label)
            } < /strong><span className="ai-tag">{pct(o.confidence)}</span > </div>
                < p > { text(o.description, 'Không có mô tả') } </p>
                < div className = "ai-muted" > BBox: { bboxText(o.bbox) } { o.tag ? ` · ${o.tag}` : '' } </div>
                    </article>
          ))
    }
    </div>
      )
}
</section>
  );
}

export function LocationsPanel({ items, onApply }: { items: SuggestedLocation[]; onApply?: (l: SuggestedLocation) => void }) {
    const apply = (l: SuggestedLocation) => {
        const ok = window.confirm(`Áp dụng "${l.name}" vào hồ sơ?\n\nĐây chỉ là gợi ý của AI, chưa được xác minh. Hãy chắc chắn bạn đã kiểm tra thực địa.`);
        if (ok) onApply?.(l);
    };
    const sorted = [...items].sort((a, b) => a.rank - b.rank);
    return (
        <section className= "ai-card" >
        <header className="ai-card__head" >
            <h3>3. Địa điểm suy luận </h3>
                < span className = "ai-tag ai-tag--warn" > AI gợi ý, chưa phải vị trí đã xác minh </span>
                    </header>
    {
        sorted.length === 0 ? <p className="ai-muted" > AI chưa gợi ý được địa điểm nào.</p> : (
            < ul className = "ai-list" >
            {
                sorted.map((l) => (
                    <li key= { l.rank } className = "ai-item" >
                    <div><span className="ai-tag" > Ưu tiên { l.rank } < /span> <strong>{text(l.name)}</strong > <span className="ai-tag" > { pct(l.confidence)
            } < /span></div >
                <div className="ai-muted" >
                { text(l.adminArea, 'Chưa rõ địa bàn') } · Tọa độ: { l.lat !== undefined && l.lng !== undefined ? `${l.lat}, ${l.lng}` : 'Chưa có' }
        </div>
        { l.basis && <div className="ai-muted" > Cơ sở suy luận: { l.basis } </div> }
        { onApply && <button className="ai-btn ai-btn--ghost" onClick = {() => apply(l) }> Áp dụng vào hồ sơ </button>
    }
    </li>
          ))
}
</ul>
      )}
</section>
  );
}

const FIELD_STATUS = { done: 'Hoàn tất', missing: 'Không có dữ liệu', out_of_scope: 'Ngoài phạm vi ảnh' } as const;

export function PartialTable({ fields }: { fields: MissingField[] }) {
    return (
        <section className= "ai-card ai-card--warn" >
        <header className="ai-card__head" > <h3>Dữ liệu khuyết thiếu một phần < /h3></header >
            <div className="ai-scroll" >
                <table className="ai-table" >
                    <thead><tr><th>Hạng mục < /th><th>Trạng thái</th > <th>Giá trị < /th><th>Khuyến nghị</th > </tr></thead >
                        <tbody>
                        {
                            fields.map((f, i) => (
                                <tr key= { i } >
                                <td>{ text(f.field)
                        } < /td><td>{FIELD_STATUS[f.status]}</td >
                        <td>{ text(f.value, '—') } < /td><td>{text(f.recommendation, '—')}</td >
                        </tr>
            ))
}
</tbody>
    </table>
    </div>
    </section>
  );
}