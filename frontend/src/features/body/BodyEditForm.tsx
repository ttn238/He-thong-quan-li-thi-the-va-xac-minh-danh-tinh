import React, { useEffect, useState } from 'react';
import bodyService from '../../services/bodyService';
import {
  BODY_GENDER_OPTIONS,
  BODY_STATUS_LABELS,
  type BodyGender,
  type BodyRecord,
} from '../../types/Body';
import {
  buildUpdatePayload,
  mapBodyToFormValues,
  validateBodyForm,
  type BodyEditFormValues,
  type BodyFormErrors,
} from './bodyRules';

interface BodyEditFormProps {
  body: BodyRecord;
  restricted: boolean;
  onCancel: () => void;
  onUpdated: (message: string, updated?: BodyRecord) => Promise<void> | void;
}

export const BodyEditForm: React.FC<BodyEditFormProps> = ({
  body,
  restricted,
  onCancel,
  onUpdated,
}) => {
  const [values, setValues] = useState<BodyEditFormValues>(() => mapBodyToFormValues(body));
  const [fieldErrors, setFieldErrors] = useState<BodyFormErrors>({});
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  // Nạp lại dữ liệu hiện tại vào form khi bản ghi thay đổi
  useEffect(() => {
    setValues(mapBodyToFormValues(body));
    setFieldErrors({});
    setSubmitError(null);
  }, [body]);

  const handleChange = <K extends keyof BodyEditFormValues>(
    field: K,
    value: BodyEditFormValues[K],
  ) => {
    setValues((prev) => ({ ...prev, [field]: value }));
    if (fieldErrors[field]) {
      setFieldErrors((prev) => ({ ...prev, [field]: undefined }));
    }
    if (submitError) setSubmitError(null);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const errors = validateBodyForm(values, restricted);
    if (Object.keys(errors).length > 0) {
      setFieldErrors(errors);
      setSubmitError('Vui lòng kiểm tra lại các thông tin nhập chưa hợp lệ.');
      return;
    }

    setSubmitting(true);
    setSubmitError(null);
    try {
      const payload = buildUpdatePayload(values, restricted);
      const res = await bodyService.updateBody(body.maHoSoThiThe, payload);
      await onUpdated(res.message || 'Cập nhật hồ sơ thi thể thành công.', res.data);
    } catch (err: any) {
      setSubmitError(err?.message || 'Cập nhật hồ sơ thất bại. Vui lòng thử lại.');
    } finally {
      setSubmitting(false);
    }
  };

  const storageText =
    body.khu || body.tu || body.ngan
      ? `Khu ${body.khu ?? '-'} / Tủ ${body.tu ?? '-'} / Ngăn ${body.ngan ?? '-'}`
      : 'Chưa xếp vị trí';

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      {submitError && (
        <div
          role="alert"
          className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700"
        >
          {submitError}
        </div>
      )}

      {/* Nhóm trường cố định / hạn chế thay đổi */}
      <div className="rounded-lg border border-slate-200 bg-slate-50 p-4">
        <div className="mb-3 flex items-center justify-between">
          <h3 className="text-sm font-semibold text-slate-700">
            Thông tin hệ thống (không thể chỉnh sửa trực tiếp)
          </h3>
          <span className="rounded bg-slate-200 px-2 py-0.5 text-xs font-medium text-slate-600">
            Chỉ đọc
          </span>
        </div>
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <div>
            <label className="block text-xs font-medium text-slate-500">Mã hồ sơ</label>
            <input
              type="text"
              value={`#${body.maHoSoThiThe}`}
              disabled
              className="mt-1 w-full rounded-md border border-slate-200 bg-slate-100 px-3 py-2 text-sm text-slate-600"
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-500">Trạng thái hồ sơ</label>
            <input
              type="text"
              value={body.nhanTrangThai || BODY_STATUS_LABELS[body.trangThai] || body.trangThai}
              disabled
              className="mt-1 w-full rounded-md border border-slate-200 bg-slate-100 px-3 py-2 text-sm text-slate-600"
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-500">Mã tin báo liên kết</label>
            <input
              type="text"
              value={body.maTinBao != null ? `#${body.maTinBao}` : 'Không có'}
              disabled
              className="mt-1 w-full rounded-md border border-slate-200 bg-slate-100 px-3 py-2 text-sm text-slate-600"
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-500">Vị trí lưu trữ</label>
            <input
              type="text"
              value={storageText}
              disabled
              className="mt-1 w-full rounded-md border border-slate-200 bg-slate-100 px-3 py-2 text-sm text-slate-600"
            />
          </div>
        </div>
      </div>

      {restricted && (
        <div className="rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800">
          Tài khoản thuộc phạm vi hạn chế (<strong>Nhà xác</strong>): chỉ được chỉnh sửa{' '}
          <strong>Giới tính</strong> và <strong>Tuổi ước lượng</strong>. Các trường Thời gian phát hiện,
          Địa điểm, Chiều cao và Đặc điểm cơ thể bị khóa.
        </div>
      )}

      {/* Nhóm trường được phép chỉnh sửa */}
      <div className="grid grid-cols-1 gap-5 md:grid-cols-2">
        <div>
          <label htmlFor="thoiGianPhatHien" className="block text-sm font-medium text-slate-700">
            Thời gian phát hiện {!restricted && <span className="text-red-500">*</span>}{' '}
            {restricted && <span className="text-xs font-normal text-amber-700">(Hạn chế)</span>}
          </label>
          <input
            id="thoiGianPhatHien"
            type="datetime-local"
            disabled={restricted}
            value={values.thoiGianPhatHien}
            onChange={(e) => handleChange('thoiGianPhatHien', e.target.value)}
            className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-blue-600 focus:outline-none focus:ring-1 focus:ring-blue-600 disabled:bg-slate-100 disabled:text-slate-500"
          />
          {fieldErrors.thoiGianPhatHien && (
            <p className="mt-1 text-xs text-red-600">{fieldErrors.thoiGianPhatHien}</p>
          )}
        </div>

        <div>
          <label htmlFor="gioiTinh" className="block text-sm font-medium text-slate-700">
            Giới tính ước tính
          </label>
          <select
            id="gioiTinh"
            value={values.gioiTinh}
            onChange={(e) => handleChange('gioiTinh', e.target.value as BodyGender)}
            className="mt-1 w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm focus:border-blue-600 focus:outline-none focus:ring-1 focus:ring-blue-600"
          >
            {BODY_GENDER_OPTIONS.map((opt) => (
              <option key={opt.value} value={opt.value}>
                {opt.label}
              </option>
            ))}
          </select>
          {fieldErrors.gioiTinh && (
            <p className="mt-1 text-xs text-red-600">{fieldErrors.gioiTinh}</p>
          )}
        </div>

        <div>
          <label htmlFor="tuoiUocLuong" className="block text-sm font-medium text-slate-700">
            Tuổi ước lượng (0 – 130)
          </label>
          <input
            id="tuoiUocLuong"
            type="number"
            min={0}
            max={130}
            step={1}
            value={values.tuoiUocLuong}
            onChange={(e) => handleChange('tuoiUocLuong', e.target.value)}
            placeholder="Ví dụ: 35"
            className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-blue-600 focus:outline-none focus:ring-1 focus:ring-blue-600"
          />
          {fieldErrors.tuoiUocLuong && (
            <p className="mt-1 text-xs text-red-600">{fieldErrors.tuoiUocLuong}</p>
          )}
        </div>

        <div>
          <label htmlFor="chieuCao" className="block text-sm font-medium text-slate-700">
            Chiều cao (cm){' '}
            {restricted && <span className="text-xs font-normal text-amber-700">(Hạn chế)</span>}
          </label>
          <input
            id="chieuCao"
            type="number"
            min={1}
            max={300}
            step="0.5"
            disabled={restricted}
            value={values.chieuCao}
            onChange={(e) => handleChange('chieuCao', e.target.value)}
            placeholder={restricted ? 'Bị ẩn theo phạm vi quyền' : 'Ví dụ: 170'}
            className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-blue-600 focus:outline-none focus:ring-1 focus:ring-blue-600 disabled:bg-slate-100 disabled:text-slate-500"
          />
          {fieldErrors.chieuCao && (
            <p className="mt-1 text-xs text-red-600">{fieldErrors.chieuCao}</p>
          )}
        </div>

        <div className="md:col-span-2">
          <label htmlFor="diaDiem" className="block text-sm font-medium text-slate-700">
            Địa điểm phát hiện {!restricted && <span className="text-red-500">*</span>}{' '}
            {restricted && <span className="text-xs font-normal text-amber-700">(Hạn chế)</span>}
          </label>
          <input
            id="diaDiem"
            type="text"
            maxLength={255}
            disabled={restricted}
            value={values.diaDiem}
            onChange={(e) => handleChange('diaDiem', e.target.value)}
            placeholder={restricted ? 'Bị ẩn theo phạm vi quyền' : 'Nhập địa điểm phát hiện thi thể'}
            className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-blue-600 focus:outline-none focus:ring-1 focus:ring-blue-600 disabled:bg-slate-100 disabled:text-slate-500"
          />
          {fieldErrors.diaDiem && (
            <p className="mt-1 text-xs text-red-600">{fieldErrors.diaDiem}</p>
          )}
        </div>

        <div className="md:col-span-2">
          <label htmlFor="dacDiemCoThe" className="block text-sm font-medium text-slate-700">
            Đặc điểm cơ thể / nhận dạng{' '}
            {restricted && <span className="text-xs font-normal text-amber-700">(Hạn chế)</span>}
          </label>
          <textarea
            id="dacDiemCoThe"
            rows={4}
            disabled={restricted}
            value={values.dacDiemCoThe}
            onChange={(e) => handleChange('dacDiemCoThe', e.target.value)}
            placeholder={
              restricted
                ? 'Bị ẩn theo phạm vi quyền'
                : 'Mô tả vết sẹo, hình xăm, trang phục, đặc điểm nhận dạng...'
            }
            className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-blue-600 focus:outline-none focus:ring-1 focus:ring-blue-600 disabled:bg-slate-100 disabled:text-slate-500"
          />
          {fieldErrors.dacDiemCoThe && (
            <p className="mt-1 text-xs text-red-600">{fieldErrors.dacDiemCoThe}</p>
          )}
        </div>
      </div>

      <div className="flex items-center justify-end gap-3 border-t border-slate-200 pt-4">
        <button
          type="button"
          onClick={onCancel}
          disabled={submitting}
          className="rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 disabled:opacity-50"
        >
          Hủy bỏ
        </button>
        <button
          type="submit"
          disabled={submitting}
          className="rounded-lg bg-[#0f2b4d] px-5 py-2 text-sm font-medium text-white hover:bg-[#1a3f6e] disabled:opacity-50"
        >
          {submitting ? 'Đang lưu...' : 'Lưu thay đổi'}
        </button>
      </div>
    </form>
  );
};

export default BodyEditForm;
