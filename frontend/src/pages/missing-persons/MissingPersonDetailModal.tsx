import React, { useCallback, useEffect, useState } from 'react';
import missingPersonService from '../../services/missingPersonService';
import {
  MISSING_STATUS_BADGES,
  MISSING_STATUS_LABELS,
  type MissingPersonDetail,
} from '../../types/MissingPerson';
import { formatDateTimeDisplay } from '../../features/body';

type DetailTab = 'info' | 'relatives' | 'images' | 'matches';

interface MissingPersonDetailModalProps {
  open: boolean;
  missingPersonId: number | null;
  onClose: () => void;
}

function formatDateOnly(iso?: string | null): string {
  if (!iso) return 'Chưa rõ';
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return iso;
  return d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' });
}

export const MissingPersonDetailModal: React.FC<MissingPersonDetailModalProps> = ({
  open,
  missingPersonId,
  onClose,
}) => {
  const [activeTab, setActiveTab] = useState<DetailTab>('info');
  const [detail, setDetail] = useState<MissingPersonDetail | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadDetail = useCallback(async () => {
    if (!missingPersonId || missingPersonId <= 0) return;
    setLoading(true);
    setError(null);
    try {
      const data = await missingPersonService.getById(missingPersonId);
      setDetail(data);
    } catch (err: any) {
      setDetail(null);
      setError(err?.message || 'Không thể tải chi tiết hồ sơ người mất tích.');
    } finally {
      setLoading(false);
    }
  }, [missingPersonId]);

  useEffect(() => {
    if (open && missingPersonId) {
      setActiveTab('info');
      loadDetail();
    }
  }, [open, missingPersonId, loadDetail]);

  if (!open) return null;

  const statusBadge =
    (detail && MISSING_STATUS_BADGES[detail.trangThai]) ||
    'bg-slate-100 text-slate-700 border-slate-200';
  const statusLabel =
    (detail && (detail.nhanTrangThai || MISSING_STATUS_LABELS[detail.trangThai] || detail.trangThai)) ||
    '';

  const tabs: { id: DetailTab; label: string }[] = [
    { id: 'info', label: 'Thông tin cá nhân' },
    { id: 'relatives', label: `Người thân (${detail?.nguoiThan.length ?? 0})` },
    { id: 'images', label: `Album ảnh (${detail?.hinhAnh.length ?? 0})` },
    { id: 'matches', label: `Lịch sử đối sánh (${detail?.ketQuaDoiSanh.length ?? 0})` },
  ];

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 p-4"
      role="dialog"
      aria-modal="true"
    >
      <div className="flex max-h-[90vh] w-full max-w-4xl flex-col overflow-hidden rounded-2xl bg-white shadow-xl">
        {/* Header */}
        <div className="flex items-start justify-between gap-4 border-b border-slate-200 bg-[#0f2b4d] px-6 py-4 text-white">
          <div>
            <p className="text-xs uppercase tracking-wider text-slate-300">
              Hồ sơ người mất tích #{missingPersonId}
            </p>
            <h2 className="mt-0.5 text-lg font-bold">
              {detail ? detail.hoTen : 'Đang tải thông tin...'}
            </h2>
          </div>

          <div className="flex items-center gap-3">
            {detail && (
              <span className={`rounded-full border px-3 py-1 text-xs font-semibold ${statusBadge}`}>
                {statusLabel}
              </span>
            )}
            <button
              type="button"
              onClick={onClose}
              className="rounded-lg bg-white/10 px-3 py-1.5 text-xs font-semibold text-white hover:bg-white/20"
            >
              Đóng
            </button>
          </div>
        </div>

        {/* Tab Bar */}
        <div className="flex flex-wrap gap-1 border-b border-slate-200 bg-slate-50 px-6 pt-2">
          {tabs.map((tab) => (
            <button
              key={tab.id}
              type="button"
              onClick={() => setActiveTab(tab.id)}
              className={`rounded-t-lg px-4 py-2.5 text-sm font-semibold transition ${
                activeTab === tab.id
                  ? 'border-x border-t border-slate-200 bg-white text-[#0f2b4d]'
                  : 'text-slate-600 hover:text-[#0f2b4d]'
              }`}
            >
              {tab.label}
            </button>
          ))}
        </div>

        {/* Body */}
        <div className="flex-1 overflow-y-auto p-6">
          {loading && (
            <p className="py-10 text-center text-sm text-slate-500">
              Đang tải dữ liệu hồ sơ người mất tích...
            </p>
          )}

          {!loading && error && (
            <div
              role="alert"
              className="rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700"
            >
              <p className="font-semibold">Không thể tải hồ sơ</p>
              <p className="mt-1">{error}</p>
              <button
                type="button"
                onClick={loadDetail}
                className="mt-3 rounded-lg border border-red-300 bg-white px-3 py-1 text-xs font-medium text-red-700"
              >
                Thử lại
              </button>
            </div>
          )}

          {!loading && !error && detail && activeTab === 'info' && (
            <dl className="grid grid-cols-1 gap-5 sm:grid-cols-2">
              <div>
                <dt className="text-xs font-medium uppercase text-slate-500">Mã hồ sơ</dt>
                <dd className="mt-1 text-sm font-semibold text-slate-900">
                  #{detail.maHoSoMatTich}
                </dd>
              </div>
              <div>
                <dt className="text-xs font-medium uppercase text-slate-500">Họ và tên</dt>
                <dd className="mt-1 text-sm font-semibold text-slate-900">{detail.hoTen}</dd>
              </div>
              <div>
                <dt className="text-xs font-medium uppercase text-slate-500">Ngày sinh / Tuổi</dt>
                <dd className="mt-1 text-sm text-slate-900">
                  {formatDateOnly(detail.ngaySinh)}
                  {detail.tuoiUocTinh != null ? ` (${detail.tuoiUocTinh} tuổi)` : ''}
                </dd>
              </div>
              <div>
                <dt className="text-xs font-medium uppercase text-slate-500">Giới tính</dt>
                <dd className="mt-1 text-sm text-slate-900">
                  {detail.nhanGioiTinh || detail.gioiTinh || 'Không xác định'}
                </dd>
              </div>
              <div>
                <dt className="text-xs font-medium uppercase text-slate-500">Thời gian mất tích</dt>
                <dd className="mt-1 text-sm text-slate-900">
                  {formatDateTimeDisplay(detail.thoiGianMatTich)}
                </dd>
              </div>
              <div>
                <dt className="text-xs font-medium uppercase text-slate-500">Trạng thái</dt>
                <dd className="mt-1 text-sm font-medium text-slate-900">{statusLabel}</dd>
              </div>
              <div className="sm:col-span-2">
                <dt className="text-xs font-medium uppercase text-slate-500">Nơi mất tích</dt>
                <dd className="mt-1 text-sm text-slate-900">
                  {detail.noiMatTich || 'Chưa cập nhật'}
                </dd>
              </div>
            </dl>
          )}

          {!loading && !error && detail && activeTab === 'relatives' && (
            detail.nguoiThan.length === 0 ? (
              <p className="rounded-lg border border-dashed border-slate-200 p-6 text-center text-sm text-slate-500">
                Chưa có thông tin người thân liên hệ.
              </p>
            ) : (
              <div className="overflow-x-auto rounded-lg border border-slate-200">
                <table className="min-w-full divide-y divide-slate-200 text-sm">
                  <thead className="bg-slate-50 text-left text-xs font-semibold uppercase text-slate-600">
                    <tr>
                      <th className="px-4 py-3">Họ tên</th>
                      <th className="px-4 py-3">Quan hệ</th>
                      <th className="px-4 py-3">Số điện thoại</th>
                      <th className="px-4 py-3">Địa chỉ</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {detail.nguoiThan.map((r) => (
                      <tr key={r.maNguoiThan}>
                        <td className="px-4 py-3 font-medium text-slate-900">{r.hoTen}</td>
                        <td className="px-4 py-3 text-slate-700">{r.quanHe || '—'}</td>
                        <td className="px-4 py-3 text-slate-700">{r.soDienThoai || '—'}</td>
                        <td className="px-4 py-3 text-slate-700">{r.diaChi || '—'}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )
          )}

          {!loading && !error && detail && activeTab === 'images' && (
            !detail.coQuyenXemHinhAnh ? (
              <p className="rounded-lg border border-amber-200 bg-amber-50 p-4 text-sm text-amber-800">
                Tài khoản của bạn không có quyền xem album hình ảnh của hồ sơ này.
              </p>
            ) : detail.hinhAnh.length === 0 ? (
              <p className="rounded-lg border border-dashed border-slate-200 p-6 text-center text-sm text-slate-500">
                Chưa có hình ảnh nào được tải lên.
              </p>
            ) : (
              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                {detail.hinhAnh.map((img) => (
                  <div
                    key={img.maHinhAnh}
                    className="rounded-xl border border-slate-200 bg-slate-50 p-4"
                  >
                    <div className="flex items-center justify-between gap-2">
                      <span className="truncate text-sm font-semibold text-[#0f2b4d]">
                        {img.tenGoc}
                      </span>
                      <span className="rounded bg-white px-2 py-0.5 text-xs text-slate-600">
                        {img.kichThuocHienThi || `${img.kichThuoc} B`}
                      </span>
                    </div>
                    <p className="mt-1 text-xs text-slate-500">
                      Loại: {img.loaiHinhAnh} · Người tải: {img.nguoiTaiLen}
                    </p>
                    {img.moTa && <p className="mt-2 text-sm text-slate-700">{img.moTa}</p>}
                    <p className="mt-2 text-xs text-slate-400">
                      {formatDateTimeDisplay(img.thoiGianTaiLen)}
                    </p>
                  </div>
                ))}
              </div>
            )
          )}

          {!loading && !error && detail && activeTab === 'matches' && (
            !detail.coQuyenXemDoiSanh ? (
              <p className="rounded-lg border border-amber-200 bg-amber-50 p-4 text-sm text-amber-800">
                Tài khoản của bạn không có quyền xem lịch sử đối sánh nghi vấn.
              </p>
            ) : detail.ketQuaDoiSanh.length === 0 ? (
              <p className="rounded-lg border border-dashed border-slate-200 p-6 text-center text-sm text-slate-500">
                Chưa phát sinh ca đối sánh nghi vấn nào với hồ sơ thi thể.
              </p>
            ) : (
              <div className="overflow-x-auto rounded-lg border border-slate-200">
                <table className="min-w-full divide-y divide-slate-200 text-sm">
                  <thead className="bg-slate-50 text-left text-xs font-semibold uppercase text-slate-600">
                    <tr>
                      <th className="px-4 py-3">Mã đối sánh</th>
                      <th className="px-4 py-3">Mã thi thể</th>
                      <th className="px-4 py-3">Điểm</th>
                      <th className="px-4 py-3">Mức độ</th>
                      <th className="px-4 py-3">Trạng thái</th>
                      <th className="px-4 py-3">Thời gian</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {detail.ketQuaDoiSanh.map((m) => (
                      <tr key={m.maDoiSanh}>
                        <td className="px-4 py-3 font-medium text-[#0f2b4d]">#{m.maDoiSanh}</td>
                        <td className="px-4 py-3 font-medium text-slate-800">#{m.maHoSoThiThe}</td>
                        <td className="px-4 py-3 font-semibold text-emerald-700">
                          {m.diemHienThi || (m.diemDoiSanh != null ? `${m.diemDoiSanh}%` : '—')}
                        </td>
                        <td className="px-4 py-3 text-slate-700">
                          {m.nhanMucDoPhuHop || m.mucDoPhuHop || '—'}
                        </td>
                        <td className="px-4 py-3 text-slate-700">
                          {m.nhanTrangThai || m.trangThai}
                        </td>
                        <td className="px-4 py-3 text-slate-500">
                          {formatDateTimeDisplay(m.thoiGianYeuCau)}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )
          )}
        </div>
      </div>
    </div>
  );
};

export default MissingPersonDetailModal;
