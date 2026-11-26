import React, { useCallback, useEffect, useState } from 'react';
import storageService from '../../services/storageService';
import type { StorageMovementItem } from '../../types/StorageLocation';
import { formatDateTimeDisplay } from './bodyRules';

interface StorageMovementTimelineProps {
  bodyId: number;
}

export const StorageMovementTimeline: React.FC<StorageMovementTimelineProps> = ({ bodyId }) => {
  const [items, setItems] = useState<StorageMovementItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadHistory = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await storageService.getMovementHistory(bodyId);
      setItems(Array.isArray(data) ? data : []);
    } catch (err: any) {
      setError(err?.message || 'Không thể tải lịch sử di chuyển vị trí lưu trữ.');
    } finally {
      setLoading(false);
    }
  }, [bodyId]);

  useEffect(() => {
    loadHistory();
  }, [loadHistory]);

  return (
    <div className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
      <div className="mb-5 flex flex-wrap items-center justify-between gap-3 border-b border-slate-200 pb-3">
        <div>
          <h2 className="text-lg font-bold text-[#0f2b4d]">
            Lịch sử di chuyển & lưu trữ ({items.length})
          </h2>
          <p className="text-xs text-slate-500">
            Dòng thời gian các lần xếp ngăn và luân chuyển vị trí lưu trữ của hồ sơ #{bodyId}.
          </p>
        </div>
        <button
          type="button"
          onClick={loadHistory}
          disabled={loading}
          className="rounded-lg border border-slate-300 bg-white px-3 py-1.5 text-xs font-medium text-slate-700 hover:bg-slate-50 disabled:opacity-50"
        >
          Làm mới
        </button>
      </div>

      {loading && (
        <p className="py-4 text-center text-sm text-slate-500">
          Đang tải dòng thời gian lưu trữ...
        </p>
      )}

      {!loading && error && (
        <div
          role="alert"
          className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700"
        >
          {error}
        </div>
      )}

      {!loading && !error && items.length === 0 && (
        <p className="rounded-lg border border-dashed border-slate-200 p-5 text-center text-sm text-slate-500">
          Hồ sơ này chưa có lịch sử xếp hoặc di chuyển vị trí lưu trữ.
        </p>
      )}

      {!loading && !error && items.length > 0 && (
        <ol className="relative ml-3 space-y-6 border-l-2 border-slate-200">
          {items.map((item, idx) => {
            const locationLabel = item.viTriDayDu || `${item.khu} - ${item.tu} - ${item.ngan}`;
            const isActive = item.laViTriHienTai;
            return (
              <li key={item.maLichSu} className="ml-6">
                <span
                  className={`absolute -left-[9px] flex h-4 w-4 items-center justify-center rounded-full ring-4 ring-white ${
                    isActive ? 'bg-emerald-500' : 'bg-slate-400'
                  }`}
                />

                <div
                  className={`rounded-lg border p-4 ${
                    isActive
                      ? 'border-emerald-300 bg-emerald-50/60 shadow-xs'
                      : 'border-slate-200 bg-slate-50/70'
                  }`}
                >
                  <div className="flex flex-wrap items-center justify-between gap-2">
                    <div className="flex flex-wrap items-center gap-2">
                      <span className="text-xs font-semibold text-slate-500">Bước #{idx + 1}</span>
                      <span className="text-sm font-bold text-[#0f2b4d]">{locationLabel}</span>
                    </div>

                    {isActive ? (
                      <span className="inline-flex items-center gap-1.5 rounded-full border border-emerald-300 bg-emerald-100 px-2.5 py-0.5 text-xs font-semibold text-emerald-800">
                        <span className="h-1.5 w-1.5 rounded-full bg-emerald-600" />
                        Vị trí hiện tại (Active)
                      </span>
                    ) : (
                      <span className="inline-flex rounded-full border border-slate-200 bg-white px-2.5 py-0.5 text-xs font-medium text-slate-600">
                        Đã chuyển đi
                      </span>
                    )}
                  </div>

                  <dl className="mt-3 grid grid-cols-1 gap-3 text-xs sm:grid-cols-2 lg:grid-cols-4">
                    <div>
                      <dt className="font-medium text-slate-500">Thời gian vào</dt>
                      <dd className="mt-0.5 font-semibold text-slate-800">
                        {formatDateTimeDisplay(item.thoiGianVao)}
                      </dd>
                    </div>
                    <div>
                      <dt className="font-medium text-slate-500">Thời gian ra</dt>
                      <dd className="mt-0.5 font-semibold text-slate-800">
                        {item.thoiGianRa ? formatDateTimeDisplay(item.thoiGianRa) : 'Đang lưu giữ'}
                      </dd>
                    </div>
                    <div>
                      <dt className="font-medium text-slate-500">Người thao tác</dt>
                      <dd className="mt-0.5 font-medium text-slate-800">{item.nguoiThaoTac}</dd>
                    </div>
                    <div>
                      <dt className="font-medium text-slate-500">Lý do</dt>
                      <dd className="mt-0.5 text-slate-700">{item.lyDo || '—'}</dd>
                    </div>
                  </dl>
                </div>
              </li>
            );
          })}
        </ol>
      )}
    </div>
  );
};

export default StorageMovementTimeline;
