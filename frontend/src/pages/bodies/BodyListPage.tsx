import React, { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import bodyService from '../../services/bodyService';
import {
  BODY_GENDER_LABELS,
  BODY_GENDER_OPTIONS,
  BODY_STATUS_BADGES,
  BODY_STATUS_LABELS,
  type BodyFilterParams,
  type BodyRecord,
} from '../../types/Body';
import { formatDateTimeDisplay } from '../../features/body';

export const BodyListPage: React.FC = () => {
  const [items, setItems] = useState<BodyRecord[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filters, setFilters] = useState<BodyFilterParams>({
    tuKhoa: '',
    trangThai: '',
    gioiTinh: '',
    trang: 1,
    soLuong: 20,
  });

  const fetchList = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await bodyService.getBodies(filters);
      setItems(res.items || []);
      setTotal(res.total || 0);
    } catch (err: any) {
      setError(err?.message || 'Không thể tải danh sách hồ sơ thi thể.');
    } finally {
      setLoading(false);
    }
  }, [filters]);

  useEffect(() => {
    fetchList();
  }, [fetchList]);

  const totalPages = Math.max(1, Math.ceil(total / (filters.soLuong || 20)));
  const currentPage = filters.trang || 1;

  return (
    <div className="min-h-screen bg-[#eef2f7] px-4 py-6 sm:px-6 lg:px-8">
      <div className="mx-auto max-w-6xl space-y-5">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <Link to="/dashboard" className="text-sm font-medium text-[#1f5fae] hover:underline">
              ← Bàn làm việc
            </Link>
            <h1 className="mt-1 text-2xl font-bold text-[#0f2b4d]">Danh sách hồ sơ thi thể</h1>
          </div>
          <span className="text-sm text-slate-600">Tổng số: {total} hồ sơ</span>
        </div>

        {/* Bộ lọc */}
        <div className="grid grid-cols-1 gap-3 rounded-xl border border-slate-200 bg-white p-4 shadow-sm sm:grid-cols-4">
          <input
            type="text"
            value={filters.tuKhoa ?? ''}
            onChange={(e) => setFilters((p) => ({ ...p, tuKhoa: e.target.value, trang: 1 }))}
            placeholder="Tìm mã hồ sơ / địa điểm / đặc điểm..."
            className="rounded-lg border border-slate-300 px-3 py-2 text-sm sm:col-span-2"
          />
          <select
            value={filters.trangThai ?? ''}
            onChange={(e) => setFilters((p) => ({ ...p, trangThai: e.target.value, trang: 1 }))}
            className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm"
          >
            <option value="">Tất cả trạng thái</option>
            {Object.entries(BODY_STATUS_LABELS).map(([k, label]) => (
              <option key={k} value={k}>
                {label}
              </option>
            ))}
          </select>
          <select
            value={filters.gioiTinh ?? ''}
            onChange={(e) => setFilters((p) => ({ ...p, gioiTinh: e.target.value, trang: 1 }))}
            className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm"
          >
            <option value="">Tất cả giới tính</option>
            {BODY_GENDER_OPTIONS.map((opt) => (
              <option key={opt.value} value={opt.value}>
                {opt.label}
              </option>
            ))}
          </select>
        </div>

        {error && (
          <div className="rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700">
            {error}
          </div>
        )}

        <div className="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm">
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-slate-200 text-sm">
              <thead className="bg-slate-50 text-left text-xs font-semibold uppercase text-slate-600">
                <tr>
                  <th className="px-4 py-3">Mã hồ sơ</th>
                  <th className="px-4 py-3">Thời gian phát hiện</th>
                  <th className="px-4 py-3">Địa điểm / Vị trí</th>
                  <th className="px-4 py-3">Giới tính / Tuổi</th>
                  <th className="px-4 py-3">Trạng thái</th>
                  <th className="px-4 py-3 text-right">Thao tác</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {loading ? (
                  <tr>
                    <td colSpan={6} className="px-4 py-8 text-center text-slate-500">
                      Đang tải dữ liệu...
                    </td>
                  </tr>
                ) : items.length === 0 ? (
                  <tr>
                    <td colSpan={6} className="px-4 py-8 text-center text-slate-500">
                      Không có hồ sơ thi thể phù hợp.
                    </td>
                  </tr>
                ) : (
                  items.map((item) => (
                    <tr key={item.maHoSoThiThe} className="hover:bg-slate-50">
                      <td className="px-4 py-3 font-semibold text-[#0f2b4d]">
                        #{item.maHoSoThiThe}
                      </td>
                      <td className="px-4 py-3 text-slate-700">
                        {formatDateTimeDisplay(item.thoiGianPhatHien)}
                      </td>
                      <td className="px-4 py-3 text-slate-700">
                        {item.diaDiem ||
                          (item.khu || item.tu || item.ngan
                            ? `Khu ${item.khu ?? '-'} / Tủ ${item.tu ?? '-'} / Ngăn ${item.ngan ?? '-'}`
                            : '—')}
                      </td>
                      <td className="px-4 py-3 text-slate-700">
                        {item.nhanGioiTinh ||
                          (item.gioiTinh ? BODY_GENDER_LABELS[item.gioiTinh] || item.gioiTinh : '—')}
                        {item.tuoiUocLuong != null ? ` · ${item.tuoiUocLuong} tuổi` : ''}
                      </td>
                      <td className="px-4 py-3">
                        <span
                          className={`inline-flex rounded-full border px-2.5 py-0.5 text-xs font-medium ${
                            BODY_STATUS_BADGES[item.trangThai] ||
                            'bg-slate-100 text-slate-700 border-slate-200'
                          }`}
                        >
                          {item.nhanTrangThai || BODY_STATUS_LABELS[item.trangThai] || item.trangThai}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-right">
                        <Link
                          to={`/bodies/${item.maHoSoThiThe}`}
                          className="inline-flex rounded-lg border border-slate-300 bg-white px-3 py-1.5 text-xs font-medium text-[#0f2b4d] hover:bg-slate-100"
                        >
                          Chi tiết / Sửa
                        </Link>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>

          <div className="flex items-center justify-between border-t border-slate-200 px-4 py-3 text-xs text-slate-600">
            <span>
              Trang {currentPage} / {totalPages}
            </span>
            <div className="flex gap-2">
              <button
                type="button"
                disabled={currentPage <= 1}
                onClick={() => setFilters((p) => ({ ...p, trang: (p.trang || 1) - 1 }))}
                className="rounded border border-slate-300 bg-white px-3 py-1 disabled:opacity-50"
              >
                Trang trước
              </button>
              <button
                type="button"
                disabled={currentPage >= totalPages}
                onClick={() => setFilters((p) => ({ ...p, trang: (p.trang || 1) + 1 }))}
                className="rounded border border-slate-300 bg-white px-3 py-1 disabled:opacity-50"
              >
                Trang sau
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default BodyListPage;
