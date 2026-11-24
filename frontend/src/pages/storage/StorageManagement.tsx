import React, { useState, useEffect, useCallback } from 'react';
import { StorageLocation, StorageLocationFilterParams } from '../../types/StorageLocation';
import storageService from '../../services/storageService';
import authHelper from '../../features/auth/authHelper';
import StorageModal from './StorageModal';

export const StorageManagement: React.FC = () => {
  const [locations, setLocations] = useState<StorageLocation[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Filters & Pagination
  const [filters, setFilters] = useState<StorageLocationFilterParams>({
    keyword: '',
    khu: '',
    pageIndex: 1,
    pageSize: 10,
  });
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [areas, setAreas] = useState<string[]>([]);

  // Modal states
  const [modalOpen, setModalOpen] = useState(false);
  const [modalMode, setModalMode] = useState<'create' | 'edit'>('create');
  const [selectedLocation, setSelectedLocation] = useState<StorageLocation | null>(null);

  // Delete state
  const [deletingId, setDeletingId] = useState<number | null>(null);

  const currentUser = authHelper.getCurrentUser();
  const isAdmin = authHelper.isAdmin();
  const isMorgue = authHelper.isMorgue();
  const canModify = isAdmin || isMorgue;

  const fetchAreas = useCallback(async () => {
    try {
      const data = await storageService.getAreas();
      setAreas(data || []);
    } catch {
      // Bỏ qua lỗi nạp areas
    }
  }, []);

  const fetchLocations = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await storageService.getLocations(filters);
      setLocations(res.items || []);
      setTotalCount(res.totalCount || 0);
      setTotalPages(res.totalPages || Math.ceil((res.totalCount || 1) / (filters.pageSize || 10)));
    } catch (err: any) {
      setError(err.message || 'Không thể tải danh sách vị trí lưu trữ.');
    } finally {
      setLoading(false);
    }
  }, [filters]);

  useEffect(() => {
    fetchAreas();
  }, [fetchAreas]);

  useEffect(() => {
    fetchLocations();
  }, [fetchLocations]);

  const handleOpenCreate = () => {
    setSelectedLocation(null);
    setModalMode('create');
    setModalOpen(true);
  };

  const handleOpenEdit = (item: StorageLocation) => {
    setSelectedLocation(item);
    setModalMode('edit');
    setModalOpen(true);
  };

  const handleDelete = async (item: StorageLocation) => {
    const confirmDelete = window.confirm(
      `Bạn có chắc chắn muốn xóa vị trí [${item.khu} - ${item.tu} - ${item.ngan}] (Mã #${item.maViTri}) không?`
    );
    if (!confirmDelete) return;

    setDeletingId(item.maViTri);
    try {
      await storageService.deleteLocation(item.maViTri);
      fetchLocations();
      fetchAreas();
    } catch (err: any) {
      alert(err.message || 'Không thể xóa vị trí này.');
    } finally {
      setDeletingId(null);
    }
  };

  return (
    <div className="p-6 bg-slate-50 min-h-screen">
      {/* Tiêu đề trang */}
      <div className="mb-6 flex flex-col md:flex-row md:items-center md:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 tracking-tight">
            QUẢN LÝ VỊ TRÍ LƯU TRỮ (STORAGE LOCATIONS)
          </h1>
          <p className="text-sm text-slate-500 mt-1">
            Quản trị danh mục phân cấp vật lý 3 tầng: Khu vực &rarr; Tủ lưu trữ &rarr; Ngăn lạnh và sức chứa
          </p>
        </div>

        {canModify && (
          <button
            onClick={handleOpenCreate}
            className="inline-flex items-center gap-2 bg-slate-900 hover:bg-slate-800 text-white text-sm font-semibold px-4 py-2.5 rounded-lg shadow-sm transition"
          >
            <span>+</span> Thêm vị trí mới
          </button>
        )}
      </div>

      {/* Thẻ thống kê tổng quan */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-6">
        <div className="bg-white p-4 rounded-xl shadow-xs border border-slate-200">
          <p className="text-xs font-semibold text-slate-500 uppercase tracking-wider">Tổng vị trí (Ngăn)</p>
          <p className="text-2xl font-bold text-slate-900 mt-1">{totalCount}</p>
        </div>

        <div className="bg-white p-4 rounded-xl shadow-xs border border-slate-200">
          <p className="text-xs font-semibold text-slate-500 uppercase tracking-wider">Khu vực lưu trữ</p>
          <p className="text-2xl font-bold text-slate-900 mt-1">{areas.length || '-'}</p>
        </div>

        <div className="bg-white p-4 rounded-xl shadow-xs border border-slate-200">
          <p className="text-xs font-semibold text-slate-500 uppercase tracking-wider">Người thực hiện</p>
          <p className="text-sm font-bold text-slate-900 mt-1 truncate">
            {currentUser?.hoTen || 'Cán bộ'} ({currentUser?.tenVaiTro || 'Nhà xác'})
          </p>
        </div>
      </div>

      {/* Toolbar lọc và tìm kiếm */}
      <div className="bg-white p-4 rounded-xl shadow-xs border border-slate-200 mb-6">
        <div className="flex flex-col sm:flex-row gap-3 items-center justify-between">
          <div className="flex flex-wrap items-center gap-3 w-full sm:w-auto">
            {/* Lọc theo Khu */}
            <div className="flex items-center gap-2">
              <label className="text-xs font-semibold text-slate-600 whitespace-nowrap">Khu vực:</label>
              <select
                value={filters.khu || ''}
                onChange={(e) =>
                  setFilters((prev) => ({ ...prev, khu: e.target.value, pageIndex: 1 }))
                }
                className="text-sm border border-slate-300 rounded-lg px-3 py-1.5 bg-white focus:ring-2 focus:ring-blue-500 outline-none"
              >
                <option value="">-- Tất cả các Khu --</option>
                {areas.map((a) => (
                  <option key={a} value={a}>
                    Khu {a}
                  </option>
                ))}
              </select>
            </div>

            {/* Tìm kiếm */}
            <div className="flex-1 sm:w-64">
              <input
                type="text"
                placeholder="Tìm Khu, Tủ, Ngăn..."
                value={filters.keyword || ''}
                onChange={(e) =>
                  setFilters((prev) => ({ ...prev, keyword: e.target.value, pageIndex: 1 }))
                }
                className="w-full text-sm border border-slate-300 rounded-lg px-3 py-1.5 focus:ring-2 focus:ring-blue-500 outline-none"
              />
            </div>
          </div>

          <div className="flex items-center gap-2 text-xs text-slate-500">
            <span>Hiển thị:</span>
            <select
              value={filters.pageSize || 10}
              onChange={(e) =>
                setFilters((prev) => ({ ...prev, pageSize: parseInt(e.target.value), pageIndex: 1 }))
              }
              className="text-xs border border-slate-300 rounded-md px-2 py-1 bg-white outline-none"
            >
              <option value="10">10 dòng</option>
              <option value="20">20 dòng</option>
              <option value="50">50 dòng</option>
            </select>
          </div>
        </div>
      </div>

      {/* Thông báo lỗi nếu có */}
      {error && (
        <div className="mb-6 bg-red-50 border border-red-200 text-red-700 text-sm p-4 rounded-xl flex items-center justify-between">
          <div className="flex items-center gap-2">
            <span>{error}</span>
          </div>
          <button onClick={fetchLocations} className="text-xs underline font-semibold hover:text-red-900">
            Thử lại
          </button>
        </div>
      )}

      {/* Bảng dữ liệu danh sách */}
      <div className="bg-white rounded-xl shadow-xs border border-slate-200 overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm text-slate-600">
            <thead className="bg-slate-900 text-white text-xs uppercase tracking-wider">
              <tr>
                <th className="py-3.5 px-4 font-semibold">Mã VT</th>
                <th className="py-3.5 px-4 font-semibold">Khu vực</th>
                <th className="py-3.5 px-4 font-semibold">Tủ lưu trữ</th>
                <th className="py-3.5 px-4 font-semibold">Ngăn chứa</th>
                <th className="py-3.5 px-4 font-semibold">Sức chứa</th>
                <th className="py-3.5 px-4 font-semibold">Vị trí đầy đủ</th>
                <th className="py-3.5 px-4 font-semibold text-right">Thao tác</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {loading ? (
                <tr>
                  <td colSpan={7} className="py-12 text-center text-slate-400">
                    <div className="flex items-center justify-center gap-2">
                      <div className="w-4 h-4 border-2 border-slate-400 border-t-transparent rounded-full animate-spin"></div>
                      <span>Đang nạp dữ liệu vị trí lưu trữ...</span>
                    </div>
                  </td>
                </tr>
              ) : locations.length === 0 ? (
                <tr>
                  <td colSpan={7} className="py-12 text-center text-slate-400">
                    Không tìm thấy vị trí lưu trữ nào phù hợp với bộ lọc.
                  </td>
                </tr>
              ) : (
                locations.map((item) => (
                  <tr key={item.maViTri} className="hover:bg-slate-50/80 transition">
                    <td className="py-3.5 px-4 font-mono text-xs text-slate-500 font-semibold">
                      #{item.maViTri}
                    </td>
                    <td className="py-3.5 px-4 font-medium text-slate-800">
                      <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold bg-blue-50 text-blue-700 border border-blue-100">
                        {item.khu}
                      </span>
                    </td>
                    <td className="py-3.5 px-4 text-slate-700">
                      <span className="font-semibold">{item.tu}</span>
                    </td>
                    <td className="py-3.5 px-4 text-slate-700 font-medium">
                      {item.ngan}
                    </td>
                    <td className="py-3.5 px-4">
                      <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-semibold bg-slate-100 text-slate-700">
                        {item.sucChua} người
                      </span>
                    </td>
                    <td className="py-3.5 px-4 font-mono text-xs text-slate-600">
                      {item.khu} - {item.tu} - {item.ngan}
                    </td>
                    <td className="py-3.5 px-4 text-right">
                      {canModify ? (
                        <div className="flex items-center justify-end gap-2">
                          <button
                            onClick={() => handleOpenEdit(item)}
                            title="Chỉnh sửa vị trí"
                            className="px-2.5 py-1 text-xs font-medium text-blue-700 bg-blue-50 hover:bg-blue-100 rounded-md transition"
                          >
                            Sửa
                          </button>
                          <button
                            onClick={() => handleDelete(item)}
                            disabled={deletingId === item.maViTri}
                            title="Xóa vị trí"
                            className="px-2.5 py-1 text-xs font-medium text-red-700 bg-red-50 hover:bg-red-100 rounded-md transition disabled:opacity-40"
                          >
                            {deletingId === item.maViTri ? 'Đang xóa...' : 'Xóa'}
                          </button>
                        </div>
                      ) : (
                        <span className="text-xs text-slate-400 italic">Chỉ xem</span>
                      )}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {/* Phân trang */}
        <div className="bg-slate-50/50 px-4 py-3 border-t border-slate-200 flex flex-col sm:flex-row items-center justify-between gap-3 text-xs text-slate-500">
          <div>
            Hiển thị <span className="font-semibold text-slate-700">{locations.length}</span> / Tổng số{' '}
            <span className="font-semibold text-slate-700">{totalCount}</span> ngăn lưu trữ
          </div>
          <div className="flex items-center gap-1.5">
            <button
              onClick={() => setFilters((p) => ({ ...p, pageIndex: (p.pageIndex || 1) - 1 }))}
              disabled={(filters.pageIndex || 1) <= 1 || loading}
              className="px-3 py-1.5 border border-slate-300 rounded-md hover:bg-white disabled:opacity-40 transition"
            >
              &larr; Trước
            </button>
            <span className="px-3 py-1.5 font-medium text-slate-700">
              Trang {filters.pageIndex || 1} / {totalPages}
            </span>
            <button
              onClick={() => setFilters((p) => ({ ...p, pageIndex: (p.pageIndex || 1) + 1 }))}
              disabled={(filters.pageIndex || 1) >= totalPages || loading}
              className="px-3 py-1.5 border border-slate-300 rounded-md hover:bg-white disabled:opacity-40 transition"
            >
              Sau &rarr;
            </button>
          </div>
        </div>
      </div>

      {/* Modal Thêm / Sửa */}
      <StorageModal
        isOpen={modalOpen}
        mode={modalMode}
        initialData={selectedLocation}
        onClose={() => setModalOpen(false)}
        onSuccess={() => {
          fetchLocations();
          fetchAreas();
        }}
      />
    </div>
  );
};

export default StorageManagement;
