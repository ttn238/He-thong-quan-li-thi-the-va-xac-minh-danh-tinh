import { useState } from 'react';
import type { ReviewDecision, ReviewInput, ReviewRecord } from '../../types/AiResult';
import { S } from './aiResultStyles';
import { dt, text } from './aiResultUtils';

export interface ReviewItem { id: string; label: string }

const DECISIONS: { value: ReviewDecision; title: string; hint: string }[] = [
    { value: 'confirm', title: 'Xác nhận kết quả', hint: 'Có giá trị tham khảo, chuyển bước tiếp theo' },
    { value: 'need_more', title: 'Yêu cầu bổ sung', hint: 'Cần thêm ảnh hoặc thông tin' },
    { value: 'reject', title: 'Bác bỏ kết quả', hint: 'Nhận diện sai lệch' },
];

interface Props {
    items: ReviewItem[];
    initial?: ReviewRecord;
    onSave: (input: ReviewInput) => Promise<unknown>;
}

export default function AiReviewForm({ items, initial, onSave }: Props) {
    const [checked, setChecked] = useState<Record<string, boolean>>(
        () => Object.fromEntries((initial?.checkedItems ?? []).map((id) => [id, true])));
    const [decision, setDecision] = useState<ReviewDecision | null>(initial?.decision ?? null);
    const [note, setNote] = useState(initial?.note ?? '');
    const [msg, setMsg] = useState<{ ok: boolean; text: string } | null>(null);
    const [saving, setSaving] = useState(false);

    const submit = async () => {
        const checkedItems = items.filter((i) => checked[i.id]).map((i) => i.id);
        // Quy tắc đề xuất: phải chọn kết luận; xác nhận thì phải đối chiếu ít nhất 1 mục; không xác nhận thì phải ghi chú
        if (!decision) return setMsg({ ok: false, text: 'Hãy chọn kết luận thẩm định.' });
        if (decision === 'confirm' && checkedItems.length === 0)
            return setMsg({ ok: false, text: 'Hãy đối chiếu và tick ít nhất một mục trước khi xác nhận.' });
        if (decision !== 'confirm' && note.trim() === '')
            return setMsg({ ok: false, text: 'Hãy ghi chú lý do cho kết luận này.' });

        setSaving(true);
        try {
            await onSave({ decision, checkedItems, note: note.trim() });
            setMsg({ ok: true, text: 'Đã lưu biên bản thẩm định.' });
        } catch (e) {
            setMsg({ ok: false, text: e instanceof Error ? e.message : 'Không lưu được biên bản.' });
        } finally {
            setSaving(false);
        }
    };

    const check = 'my-1.5 flex cursor-pointer items-start gap-2 text-sm';
    return (
        <section className= { S.card } >
        <header className={ S.head }> <h3 className={ S.title }> Thẩm định của cán bộ thụ lý < /h3></header >
            <p className={ S.muted }> Kết quả AI chỉ là gợi ý.Hãy đối chiếu từng mục với thực tế trước khi kết luận.</p>
    {
        initial && (
            <p className="my-2 rounded bg-blue-50 px-3 py-2 text-[13px] text-navy" >
                Đã có biên bản thẩm định({ text(initial.reviewer, 'không rõ người lập')
    }, { dt(initial.reviewedAt) }). Lưu lại sẽ ghi đè biên bản này.
        </p>
      )
}
<div className="grid grid-cols-1 gap-5 md:grid-cols-2" >
    <div>
    <h4 className="my-2 text-sm font-semibold" > Danh mục đối chiếu </h4>
{ items.length === 0 && <p className={ S.muted }> Không có mục nào để đối chiếu.</p> }
{
    items.map((i) => (
        <label key= { i.id } className = { check } >
        <input type="checkbox" className = "mt-0.5 accent-navy" checked = {!!checked[i.id]}
onChange = {(e) => setChecked((c) => ({ ...c, [i.id]: e.target.checked }))} />
    < span > { i.label } </span>
    </label>
          ))}
</div>
    < div >
    <h4 className="my-2 text-sm font-semibold" > Kết luận </h4>
{
    DECISIONS.map((d) => (
        <label key= { d.value } className = { check } >
        <input type="radio" name = "ai-decision" className = "mt-0.5 accent-navy"
                checked = { decision === d.value} onChange = {() => setDecision(d.value)} />
    < span > <strong>{ d.title } < /strong><br / > <span className={ S.muted }> { d.hint } < /span></span >
        </label>
          ))}
<textarea rows={ 4 } placeholder = "Ghi chú thẩm định và đề xuất nghiệp vụ..."
className = "my-2 w-full resize-y rounded border border-line p-2 text-sm"
value = { note } onChange = {(e) => setNote(e.target.value)} />
{ msg && <p role="alert" className = {`my-1.5 text-[13px] ${msg.ok ? 'text-green-700' : 'text-red-700'}` }> { msg.text } </p>}
<button className={ S.btn } disabled = { saving } onClick = {() => void submit()}> { saving? 'Đang lưu...': 'Lưu biên bản thẩm định' } </button>
    </div>
    </div>
    </section>
  );
}