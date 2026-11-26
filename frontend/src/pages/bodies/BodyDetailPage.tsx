import React, { useCallback, useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import bodyService from '../../services/bodyService';
import {
  BODY_GENDER_LABELS,
  BODY_STATUS_BADGES,
  BODY_STATUS_LABELS,
  type BodyRecord,
} from '../../types/Body';
import {
  BodyEditForm,
  canEditBody,
  formatDateTimeDisplay,
  isBodyClosed,
  isRestrictedBodyEditor,
} from '../../features/body';

export const BodyDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const bodyId = Number(id);

  const [body, setBody] = useState<BodyRecord | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isEditing, setIsEditing] = useState(false);
  const [feedback, setFeedback] = useState<{ type: 'success' | 'error'; text: string } | null>(null);

  const allowEdit = canEditBody();
  const restricted = isRestrictedBodyEditor();

  const fetchDetail = useCallback(async () => {
    if (!Number.isInteger(bodyId) || bodyId <= 0) {
      setError('Mã hồ sơ thi thể không hợp lệ.');
      setLoading(false);
      return;
    }

    setLoading(true);
    setError(null);
    try {
      const data = await bodyService.getBodyById(bodyId);
      setBody(data);
    } catch (err: any) {
      setBody(null);
      setError(err?.message || 'Không thể tải thông tin hồ sơ thi thể.');
    } finally {
      setLoading(false);
    }
  }, [bodyId]);

  useEffect(() => {
    fetchDetail();
  }, [fetchDetail]);

  const handleUpdated = async (message: string, updated?: BodyRecord) => {
    if (updated) setBody(updated);
    setIsEditing(false);
    setFeedback({ type: 'success', text: message });
    // Làm mới dữ liệu từ backend sau khi cập nhật
    await fetchDetail();
  };

  const closed = isBodyClosed(body);
  const statusBadgeClass =
    (body && BODY_STATUS_BADGES[body.trangThai]) || 'bg-slate-100 text-slate-700 border-slate-200';
  const statusLabel =
    (body && (body.nhanTrangThai || BODY_STATUS_LABELS[body.trangThai] || body.trangThai)) || '';

  return (
    <div className="min-h-screen bg-[#eef2f7] px-4 py-6 sm:px-6 lg:px-8">
      <div className="mx-auto max-w-5xl space-y-5">
        {/* Thanh điều hướng trên cùng */}
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <Link
              to="/bodies"
              className="text-sm font-medium text-[#1f5fae] hover:underline"
            >
              ← Quay lại danh sách hồ sơ thi thể
            </Link>
            <h1 className="mt-1 text-2xl font-bold text-[#0f2b4d]">
              Chi tiết hồ sơ thi thể {Number.isInteger(bodyId) && bodyId > 0 ? `#${bodyId}` : ''}
            </h1>
          </div>

          {body && !isEditing && (
            <div className="flex items-center gap-2">
              {allowEdit && !closed ? (
                <button
                  type="button"
                  onClick={() => {
                    setFeedback(null);
                    setIsEditing(true);
                  }}
                  className="rounded-lg bg-[#0f2b4d] px-4 py-2 text-sm font-medium text-white shadow-sm hover:bg-[#1a3f6e]"
                >
                  Chỉnh sửa hồ sơ
                </button>
              ) : (
                <span className="rounded-lg border border-slate-200 bg-white px-3 py-2 text-xs text-slate-600">
                  {closed
                    ? 'Hồ sơ đã đóng — không thể chỉnh sửa'
                    : 'Tài khoản của bạn chỉ có quyền xem'}
                </span>
              )}
            </div>
          )}
        </div>

        {feedback && (
          <div
            role="status"
            className={`flex items-center justify-between rounded-lg border px-4 py-3 text-sm ${
              feedback.type === 'success'
                ? 'border-emerald-200 bg-emerald-50 text-emerald-800'
                : 'border-red-200 bg-red-50 text-red-700'
            }`}
          >
            <span>{feedback.text}</span>
            <button
              type="button"
              onClick={() => setFeedback(null)}
              className="ml-4 text-xs font-semibold underline"
            >
              Đóng
            </button>
          </div>
        )}

        {loading && (
          <div className="rounded-xl border border-slate-200 bg-white p-8 text-center text-sm text-slate-500">
            Đang tải thông tin hồ sơ thi thể...
          </div>
        )}

        {!loading && error && (
          <div
            role="alert"
            className="rounded-xl border border-red-200 bg-red-50 p-6 text-sm text-red-700"
          >
            <p className="font-semibold">Không thể hiển thị hồ sơ</p>
            <p className="mt-1">{error}</p>
            <button
              type="button"
              onClick={fetchDetail}
              className="mt-4 rounded-lg border border-red-300 bg-white px-3 py-1.5 text-xs font-medium text-red-700 hover:bg-red-50"
            >
              Thử tải lại
            </button>
          </div>
        )}

        {!loading && !error && body && (
          <div className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
            <div className="mb-6 flex flex-wrap items-center justify-between gap-3 border-b border-slate-200 pb-4">
              <div>
                <p className="text-xs font-medium uppercase tracking-wide text-slate-500">
                  Hồ sơ tử thi
                </p>
                <p className="text-lg font-bold text-[#0f2b4d]">Mã #{body.maHoSoThiThe}</p>
              </div>
              <span
                className={`inline-flex items-center rounded-full border px-3 py-1 text-xs font-semibold ${statusBadgeClass}`}
              >
                {statusLabel}
              </span>
            </div>

            {isEditing ? (
              <BodyEditForm
                body={body}
                restricted={restricted}
                onCancel={() => setIsEditing(false)}
                onUpdated={handleUpdated}
              />
            ) : (
              <div className="space-y-6">
                <dl className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3">
                  <div>
                    <dt className="text-xs font-medium uppercase text-slate-500">
                      Thời gian phát hiện
                    </dt>
                    <dd className="mt-1 text-sm font-medium text-slate-900">
                      {formatDateTimeDisplay(body.thoiGianPhatHien)}
                    </dd>
                  </div>

                  <div>
                    <dt className="text-xs font-medium uppercase text-slate-500">
                      Giới tính ước tính
                    </dt>
                    <dd className="mt-1 text-sm font-medium text-slate-900">
                      {body.nhanGioiTinh ||
                        (body.gioiTinh ? BODY_GENDER_LABELS[body.gioiTinh] || body.gioiTinh : 'Không xác định')}
                    </dd>
                  </div>

                  <div>
                    <dt className="text-xs font-medium uppercase text-slate-500">
                      Tuổi ước lượng
                    </dt>
                    <dd className="mt-1 text-sm font-medium text-slate-900">
                      {body.tuoiUocLuong != null ? `${body.tuoiUocLuong} tuổi` : 'Chưa xác định'}
                    </dd>
                  </div>

                  <div>
                    <dt className="text-xs font-medium uppercase text-slate-500">Chiều cao</dt>
                    <dd className="mt-1 text-sm font-medium text-slate-900">
                      {body.chieuCao != null ? `${body.chieuCao} cm` : restricted ? 'Ẩn theo quyền' : 'Chưa xác định'}
                    </dd>
                  </div>

                  <div>
                    <dt className="text-xs font-medium uppercase text-slate-500">
                      Mã tin báo liên kết
                    </dt>
                    <dd className="mt-1 text-sm font-medium text-slate-900">
                      {body.maTinBao != null ? `#${body.maTinBao}` : 'Không có'}
                    </dd>
                  </div>

                  <div>
                    <dt className="text-xs font-medium uppercase text-slate-500">
                      Vị trí lưu trữ
                    </dt>
                    <dd className="mt-1 text-sm font-medium text-slate-900">
                      {body.khu || body.tu || body.ngan
                        ? `Khu ${body.khu ?? '-'} / Tủ ${body.tu ?? '-'} / Ngăn ${body.ngan ?? '-'}`
                        : 'Chưa xếp vị trí'}
                    </dd>
                  </div>

                  <div className="sm:col-span-2 lg:col-span-3">
                    <dt className="text-xs font-medium uppercase text-slate-500">
                      Địa điểm phát hiện
                    </dt>
                    <dd className="mt-1 text-sm text-slate-900">
                      {body.diaDiem || (restricted ? 'Ẩn theo phạm vi quyền nhà xác' : 'Chưa cập nhật')}
                    </dd>
                  </div>

                  <div className="sm:col-span-2 lg:col-span-3">
                    <dt className="text-xs font-medium uppercase text-slate-500">
                      Đặc điểm cơ thể / nhận dạng
                    </dt>
                    <dd className="mt-1 whitespace-pre-wrap rounded-lg border border-slate-100 bg-slate-50 p-3 text-sm text-slate-800">
                      {body.dacDiemCoThe ||
                        (restricted ? 'Ẩn theo phạm vi quyền nhà xác' : 'Chưa có mô tả đặc điểm.')}
                    </dd>
                  </div>
                </dl>
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
};

export default BodyDetailPage;
