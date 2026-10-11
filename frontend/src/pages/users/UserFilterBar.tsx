import React, { useState } from 'react';
import { UserFilterParams } from '../../types/User';

interface UserFilterBarProps {
  onFilterChange: (filters: UserFilterParams) => void;
  isLoading?: boolean;
}

export const UserFilterBar: React.FC<UserFilterBarProps> = ({ onFilterChange, isLoading }) => {
  const [keyword, setKeyword] = useState('');
  const [maVaiTro, setMaVaiTro] = useState<number | undefined>(undefined);
  const [trangThai, setTrangThai] = useState<string>('');
  const [donVi, setDonVi] = useState<string>('');

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    onFilterChange({
      keyword: keyword.trim() || undefined,
      maVaiTro: maVaiTro ? Number(maVaiTro) : undefined,
      trangThai: trangThai || undefined,
      donVi: donVi.trim() || undefined,
      pageIndex: 1,
    });
  };

  const handleReset = () => {
    setKeyword('');
    setMaVaiTro(undefined);
    setTrangThai('');
    setDonVi('');
    onFilterChange({ pageIndex: 1 });
  };

  return (
    <form onSubmit={handleSearch} className="user-filter-bar bg-white p-4 rounded-lg shadow-sm border mb-4">
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        {/* Tìm kiếm từ khóa */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Tìm kiếm</label>
          <input
            type="text"
            className="w-full px-3 py-2 border rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            placeholder="Tên đăng nhập, họ tên, email, CCCD..."
            value={keyword}
            onChange={(e) => setKeyword(e.target.value)}
          />
        </div>

        {/* Lọc vai trò */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Vai trò</label>
          <select
            className="w-full px-3 py-2 border rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            value={maVaiTro ?? ''}
            onChange={(e) => setMaVaiTro(e.target.value ? Number(e.target.value) : undefined)}
          >
            <option value="">-- Tất cả vai trò --</option>
            <option value="1">Quản trị viên (Admin)</option>
            <option value="2">Cán bộ giám định</option>
            <option value="3">Người dân</option>
          </select>
        </div>

        {/* Lọc trạng thái */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Trạng thái</label>
          <select
            className="w-full px-3 py-2 border rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            value={trangThai}
            onChange={(e) => setTrangThai(e.target.value)}
          >
            <option value="">-- Tất cả trạng thái --</option>
            <option value="HoatDong">Hoạt động</option>
            <option value="Khoa">Đang bị khóa</option>
            <option value="KhoaTam">Khóa tạm thời</option>
          </select>
        </div>

        {/* Lọc đơn vị */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Đơn vị công tác</label>
          <input
            type="text"
            className="w-full px-3 py-2 border rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            placeholder="Phòng Kỹ thuật hình sự..."
            value={donVi}
            onChange={(e) => setDonVi(e.target.value)}
          />
        </div>
      </div>

      <div className="flex justify-end gap-2 mt-4">
        <button
          type="button"
          onClick={handleReset}
          className="px-4 py-2 border border-gray-300 rounded-md text-sm text-gray-700 hover:bg-gray-50 transition"
          disabled={isLoading}
        >
          Đặt lại bộ lọc
        </button>
        <button
          type="submit"
          className="px-4 py-2 bg-blue-600 text-white rounded-md text-sm hover:bg-blue-700 transition"
          disabled={isLoading}
        >
          {isLoading ? 'Đang lọc...' : 'Áp dụng lọc'}
        </button>
      </div>
    </form>
  );
};

export default UserFilterBar;
