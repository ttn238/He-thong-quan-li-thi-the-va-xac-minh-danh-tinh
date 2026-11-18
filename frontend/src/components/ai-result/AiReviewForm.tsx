import { useState } from 'react';
import type { ReviewDecision, ReviewInput, ReviewRecord } from '../../types/AiResult';
import { S } from './aiResultStyles';
import { dt, text } from './aiResultUtils';

export interface ReviewItem { id: string; label: string }
const DECISIONS: { value: ReviewDecision; title: string; hint: string }[] = [
    { value: 'confirm', title: 'Xác nhận kết quả tham khảo', hint: 'Đã đối chiếu các mục cần thiết' },
    { value: 'need_more', title: 'Yêu cầu bổ sung', hint: 'Cần thêm ảnh hoặc thông tin' },
    { value: 'reject', title: 'Bác bỏ kết quả', hint: 'Nhận diện không phù hợp' },
];

export default function AiReviewForm({ items, initial, onSave }: {
    items: ReviewItem[]; initial?: ReviewRecord; onSave: (input: ReviewInput) => Promise<unknown>;
}) {
    const [checked, setChecked] = useState<Record<string, boolean>>(
        () => Object.fromEntries((initial?.checkedItems ?? []).map((id) => [id, true])));
    const [decision, setDecision] = useState<ReviewDecision | null>(initial?.decision ?? null);
    const [note, setNote] = useState(initial?.note ?? '');
    const [message, setMessage] = useState<{ ok: boolean; text: string } | null>(null);
    const [saving, setSaving] = useState(false);

    const submit = async () => {
        const checkedItems = items.filter((item) => checked[item.id]).map((item) => item.id);
        if (!decision) return setMessage({ ok: false, text: 'Hãy chọn kết luận thẩm định.' });
        if (decision === 'confirm' && checkedItems.length === 0)
            return setMessage({ ok: false, text: 'Hãy đối chiếu và chọn ít nhất một mục trước khi xác nhận.' });
        if (decision !== 'confirm' && !note.trim())
            return setMessage({ ok: false, text: 'Hãy ghi lý do cho kết luận này.' });
        setSaving(true);
        try {
            await onSave({ decision, checkedItems, note: note.trim() });
            setMessage({ ok: true, text: 'Đã lưu biên bản thẩm định.' });
        } catch (e) {
            setMessage({ ok: false, text: e instanceof Error ? e.message : 'Không lưu được biên bản.' });
        } finally { setSaving(false); }
    };

    return <section className={S.card}>
        <header className={S.head}><h3 className={S.title}>Thẩm định của cán bộ</h3></header>
        <p className={S.muted}>Kết quả AI chỉ là gợi ý. Đối chiếu từng mục với thực tế trước khi kết luận.</p>
        {initial && <p className="my-2 rounded bg-blue-50 px-3 py-2 text-[13px] text-navy">
            Đã có biên bản ({text(initial.reviewer, 'cán bộ')} · {dt(initial.reviewedAt)}). Lưu lại sẽ cập nhật biên bản.
        </p>}
        <div className="grid grid-cols-1 gap-5 md:grid-cols-2">
            <div>
                <h4 className="my-2 text-sm font-semibold">Danh mục đối chiếu</h4>
                {items.length === 0 && <p className={S.muted}>Không có mục nào để đối chiếu.</p>}
                {items.map((item) => <label key={item.id} className="my-1.5 flex cursor-pointer items-start gap-2 text-sm">
                    <input type="checkbox" className="mt-0.5 accent-navy" checked={!!checked[item.id]}
                        onChange={(e) => setChecked((value) => ({ ...value, [item.id]: e.target.checked }))} />
                    <span>{item.label}</span>
                </label>)}
            </div>
            <div>
                <h4 className="my-2 text-sm font-semibold">Kết luận</h4>
                {DECISIONS.map((item) => <label key={item.value} className="my-1.5 flex cursor-pointer items-start gap-2 text-sm">
                    <input type="radio" name="ai-decision" className="mt-0.5 accent-navy"
                        checked={decision === item.value} onChange={() => setDecision(item.value)} />
                    <span><strong>{item.title}</strong><br /><span className={S.muted}>{item.hint}</span></span>
                </label>)}
                <textarea rows={4} placeholder="Ghi chú thẩm định…" className="my-2 w-full resize-y rounded border border-line p-2 text-sm"
                    value={note} onChange={(e) => setNote(e.target.value)} />
                {message && <p role="status" className={`my-1.5 text-[13px] ${message.ok ? 'text-green-700' : 'text-red-700'}`}>{message.text}</p>}
                <button className={S.btn} disabled={saving} onClick={() => void submit()}>
                    {saving ? 'Đang lưu…' : 'Lưu biên bản thẩm định'}
                </button>
            </div>
        </div>
    </section>;
}
