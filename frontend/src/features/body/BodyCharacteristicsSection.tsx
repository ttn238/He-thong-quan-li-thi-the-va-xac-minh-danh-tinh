import React, { useEffect, useState } from 'react';
import bodyService from '../../services/bodyService';
import {
  BODY_CHARACTERISTIC_TYPE_LABELS,
  BODY_CHARACTERISTIC_TYPE_OPTIONS,
  type BodyCharacteristic,
  type BodyCharacteristicType,
} from '../../types/Body';
import { formatDateTimeDisplay, toDateTimeLocalValue } from './bodyRules';

interface BodyCharacteristicsSectionProps {
  bodyId: number;
  initialItems?: BodyCharacteristic[];
  canManage: boolean;
  restricted: boolean;
  closed: boolean;
  onChanged: () => Promise<void> | void;
}

interface FormState {
  loaiDacDiem: BodyCharacteristicType;
  viTriCoThe: string;
  moTa: string;
  ngayGhiNhan: string;
}

const emptyForm = (): FormState => ({
  loaiDacDiem: 'Seo',
  viTriCoThe: '',
  moTa: '',
  ngayGhiNhan: toDateTimeLocalValue(new Date().toISOString()),
});

export const BodyCharacteristicsSection: React.FC<BodyCharacteristicsSectionProps> = ({
  bodyId,
  initialItems = [],
  canManage,
  restricted,
  closed,
  onChanged,
}) => {
  const [items, setItems] = useState<BodyCharacteristic[]>(initialItems);
  const [formOpen, setFormOpen] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [form, setForm] = useState<FormState>(emptyForm);
  const [submitting, setSubmitting] = useState(false);
  const [feedback, setFeedback] = useState<{ type: 'success' | 'error'; text: string } | null>(null);

  useEffect(() => {
    setItems(initialItems);
  }, [initialItems]);

  const openCreate = () => {
    setEditingId(null);
    setForm(emptyForm());
    setFeedback(null);
    setFormOpen(true);
  };

  const openEdit = (item: BodyCharacteristic) => {
    setEditingId(item.maDacDiem);
    const validTypes = BODY_CHARACTERISTIC_TYPE_OPTIONS.map((o) => o.value);
    const loai = validTypes.includes(item.loaiDacDiem as BodyCharacteristicType)
      ? (item.loaiDacDiem as BodyCharacteristicType)
      : 'Khac';
    setForm({
      loaiDacDiem: loai,
      viTriCoThe: item.viTriCoThe ?? '',
      moTa: item.moTa,
      ngayGhiNhan: toDateTimeLocalValue(item.ngayGhiNhan),
    });
    setFeedback(null);
    setFormOpen(true);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const moTa = form.moTa.trim();
    const viTri = form.viTriCoThe.trim();

    if (!moTa) {
      setFeedback({ type: 'error', text: 'Mô tả đặc điểm nhận dạng không được để trống.' });
      return;
    }
    if (moTa.length > 500) {
      setFeedback({ type: 'error', text: 'Mô tả đặc điểm nhận dạng tối đa 500 ký tự.' });
      return;
    }
    if (viTri.length > 150) {
      setFeedback({ type: 'error', text: 'Vị trí trên cơ thể tối đa 150 ký tự.' });
      return;
    }
    if (form.ngayGhiNhan) {
      const dt = new Date(form.ngayGhiNhan);
      if (Number.isNaN(dt.getTime()) || dt.getTime() > Date.now()) {
        setFeedback({ type: 'error', text: 'Ngày ghi nhận không hợp lệ hoặc nằm ở tương lai.' });
        return;
      }
    }

    setSubmitting(true);
    setFeedback(null);
    try {
      const payload = {
        loaiDacDiem: form.loaiDacDiem,
        viTriCoThe: viTri || undefined,
        moTa,
        ngayGhiNhan: form.ngayGhiNhan || undefined,
      };

      const res = editingId
        ? await bodyService.updateCharacteristic(bodyId, editingId, payload)
        : await bodyService.createCharacteristic(bodyId, payload);

      setFormOpen(false);
      setEditingId(null);
      setFeedback({ type: 'success', text: res.message || 'Đã lưu đặc điểm nhận dạng.' });
      await onChanged();
    } catch (err: any) {
      setFeedback({ type: 'error', text: err?.message || 'Không thể lưu đặc điểm nhận dạng.' });
    } finally {
      setSubmitting(false);
    }
  };

  if (restricted) {
    return (
      <div className="rounded-xl border border-amber-200 bg-amber-50 p-5 text-sm text-amber-800">
        <h2 className="font-semibold text-amber-900">Đặc điểm nhận dạng</h2>
        <p className="mt-1">
          Thông tin đặc điểm nhận dạng chi tiết được ẩn theo phạm vi quyền hạn chế của vai trò Nhà xác.
        </p>
      </div>
    );
  }

  return (
    <div className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
      <div className="mb-4 flex flex-wrap items-center justify-between gap-3 border-b border-slate-200 pb-3">
        <div>
          <h2 className="text-lg font-bold text-[#0f2b4d]">
            Đặc điểm nhận dạng ({items.length})
          </h2>
          <p className="text-xs text-slate-500">
            Quản lý từng dấu hiệu nhận dạng (vết sẹo, hình xăm, vết bớt, răng, dị tật...) gắn với hồ sơ #{bodyId}.
          </p>
        </div>

        {canManage && !closed && !formOpen && (
          <button
            type="button"
            onClick={openCreate}
            className="rounded-lg bg-[#0f2b4d] px-3.5 py-2 text-xs font-semibold text-white hover:bg-[#1a3f6e]"
          >
            + Thêm đặc điểm
          </button>
        )}
      </div>

      {feedback && (
        <div
          role="status"
          className={`mb-4 rounded-lg border px-4 py-2.5 text-sm ${
            feedback.type === 'success'
              ? 'border-emerald-200 bg-emerald-50 text-emerald-800'
              : 'border-red-200 bg-red-50 text-red-700'
          }`}
        >
          {feedback.text}
        </div>
      )}

      {formOpen && (
        <form
          onSubmit={handleSubmit}
          className="mb-5 space-y-4 rounded-lg border border-slate-200 bg-slate-50 p-4"
        >
          <div className="text-sm font-semibold text-[#0f2b4d]">
            {editingId ? `Cập nhật đặc điểm #${editingId}` : 'Thêm đặc điểm nhận dạng mới'}
          </div>

          <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
            <div>
              <label className="block text-xs font-medium text-slate-700">
                Loại đặc điểm <span className="text-red-500">*</span>
              </label>
              <select
                value={form.loaiDacDiem}
                onChange={(e) =>
                  setForm((p) => ({ ...p, loaiDacDiem: e.target.value as BodyCharacteristicType }))
                }
                className="mt-1 w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm"
              >
                {BODY_CHARACTERISTIC_TYPE_OPTIONS.map((opt) => (
                  <option key={opt.value} value={opt.value}>
                    {opt.label}
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label className="block text-xs font-medium text-slate-700">Vị trí trên cơ thể</label>
              <input
                type="text"
                maxLength={150}
                value={form.viTriCoThe}
                onChange={(e) => setForm((p) => ({ ...p, viTriCoThe: e.target.value }))}
                placeholder="Vd: Cẳng tay trái, bả vai phải..."
                className="mt-1 w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm"
              />
            </div>

            <div>
              <label className="block text-xs font-medium text-slate-700">Ngày ghi nhận</label>
              <input
                type="datetime-local"
                value={form.ngayGhiNhan}
                onChange={(e) => setForm((p) => ({ ...p, ngayGhiNhan: e.target.value }))}
                className="mt-1 w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm"
              />
            </div>

            <div className="sm:col-span-3">
              <label className="block text-xs font-medium text-slate-700">
                Mô tả chi tiết <span className="text-red-500">*</span>
              </label>
              <textarea
                rows={3}
                maxLength={500}
                value={form.moTa}
                onChange={(e) => setForm((p) => ({ ...p, moTa: e.target.value }))}
                placeholder="Mô tả kích thước, hình dáng, màu sắc đặc điểm..."
                className="mt-1 w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm"
              />
            </div>
          </div>

          <div className="flex justify-end gap-2">
            <button
              type="button"
              disabled={submitting}
              onClick={() => {
                setFormOpen(false);
                setEditingId(null);
              }}
              className="rounded-lg border border-slate-300 bg-white px-3.5 py-1.5 text-xs font-medium text-slate-700 hover:bg-slate-100"
            >
              Hủy
            </button>
            <button
              type="submit"
              disabled={submitting}
              className="rounded-lg bg-[#0f2b4d] px-4 py-1.5 text-xs font-semibold text-white hover:bg-[#1a3f6e] disabled:opacity-50"
            >
              {submitting ? 'Đang lưu...' : editingId ? 'Lưu cập nhật' : 'Thêm đặc điểm'}
            </button>
          </div>
        </form>
      )}

      {items.length === 0 ? (
        <p className="rounded-lg border border-dashed border-slate-200 p-4 text-center text-sm text-slate-500">
          Chưa có đặc điểm nhận dạng chi tiết nào được ghi nhận cho hồ sơ này.
        </p>
      ) : (
        <div className="divide-y divide-slate-100">
          {items.map((item) => (
            <div key={item.maDacDiem} className="flex flex-wrap items-start justify-between gap-3 py-3">
              <div className="space-y-1">
                <div className="flex flex-wrap items-center gap-2">
                  <span className="rounded-full bg-blue-50 px-2.5 py-0.5 text-xs font-semibold text-[#0f2b4d]">
                    {item.nhanLoaiDacDiem ||
                      BODY_CHARACTERISTIC_TYPE_LABELS[item.loaiDacDiem] ||
                      item.loaiDacDiem}
                  </span>
                  {item.viTriCoThe && (
                    <span className="text-xs font-medium text-slate-600">
                      Vị trí: {item.viTriCoThe}
                    </span>
                  )}
                  <span className="text-xs text-slate-400">
                    Ghi nhận: {formatDateTimeDisplay(item.ngayGhiNhan)}
                  </span>
                </div>
                <p className="text-sm text-slate-800">{item.moTa}</p>
              </div>

              {canManage && !closed && (
                <button
                  type="button"
                  onClick={() => openEdit(item)}
                  className="rounded-lg border border-slate-300 bg-white px-3 py-1 text-xs font-medium text-[#0f2b4d] hover:bg-slate-50"
                >
                  Sửa
                </button>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  );
};

export default BodyCharacteristicsSection;
