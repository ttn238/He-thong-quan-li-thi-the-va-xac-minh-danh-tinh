import React, { useState, useEffect, useCallback } from 'react';
import { User, UserFilterParams, CreateUserPayload, UpdateUserPayload } from '../../types/User';
import userService from '../../services/userService';
import authHelper from '../../features/auth/authHelper';
import UserFilterBar from './UserFilterBar';
import UserModal from './UserModal';

export const UserManagement: React.FC = () => {
  const [users, setUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Pagination & filter state
  const [filters, setFilters] = useState<UserFilterParams>({
    pageIndex: 1,
    pageSize: 10,
  });
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(1);

  // Modal states
  const [modalOpen, setModalOpen] = useState(false);
  const [modalMode, setModalMode] = useState<'create' | 'edit'>('create');
  const [selectedUser, setSelectedUser] = useState<User | null>(null);

  // Reset Password State
  const [resetModalOpen, setResetModalOpen] = useState(false);
  const [newPassword, setNewPassword] = useState('');
  const [resettingUser, setResettingUser] = useState<User | null>(null);

  const currentUser = authHelper.getCurrentUser();
  const isAdmin = authHelper.isAdmin();

  const fetchUsers = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await userService.getUsers(filters);
      setUsers(res.items);
      setTotalCount(res.totalCount);
      setTotalPages(res.totalPages || Math.ceil(res.totalCount / (filters.pageSize || 10)));
    } catch (err: any) {
      setError(err.message || 'Không thể tải danh sách tài khoản.');
    } finally {
      setLoading(false);
    }
  }, [filters]);

  useEffect(() => {
    fetchUsers();
  }, [fetchUsers]);

  const handleFilterChange = (newFilters: UserFilterParams) => {
    setFilters((prev) => ({ ...prev, ...newFilters }));
  };

  const handlePageChange = (newPage: number) => {
    if (newPage >= 1 && newPage <= totalPages) {
      setFilters((prev) => ({ ...prev, pageIndex: newPage }));
    }
  };

  const handleOpenCreateModal = () => {
    setSelectedUser(null);
    setModalMode('create');
    setModalOpen(true);
  };

  const handleOpenEditModal = (user: User) => {
    setSelectedUser(user);
    setModalMode('edit');
    setModalOpen(true);
  };

  const handleModalSubmit = async (payload: CreateUserPayload | UpdateUserPayload) => {
    if (modalMode === 'create') {
      await userService.createUser(payload as CreateUserPayload);
    } else if (selectedUser) {
      await userService.updateUser(selectedUser.maTaiKhoan, payload as UpdateUserPayload);
    }
    await fetchUsers();
  };

  const handleToggleStatus = async (user: User) => {
    if (user.maTaiKhoan === currentUser?.maTaiKhoan && user.trangThai === 'HoatDong') {
      alert('Bạn không thể tự khóa tài khoản của chính mình!');
      return;
    }

    const nextStatus = user.trangThai === 'HoatDong' ? 'Khoa' : 'HoatDong';
    const actionText = nextStatus === 'Khoa' ? 'khóa' : 'mở khóa kích hoạt';

    if (!window.confirm(`Bạn có chắc chắn muốn ${actionText} tài khoản "${user.tenDangNhap}"?`)) {
      return;
    }

    try {
      await userService.changeStatus(user.maTaiKhoan, { trangThai: nextStatus });
      await fetchUsers();
    } catch (err: any) {
      alert(err.message || 'Thao tác thay đổi trạng thái thất bại.');
    }
  };

  const handleOpenResetModal = (user: User) => {
    setResettingUser(user);
    setNewPassword('');
    setResetModalOpen(true);
  };

  const handleConfirmResetPassword = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!resettingUser) return;
    if (!newPassword || newPassword.length < 6) {
      alert('Mật khẩu mới phải có tối thiểu 6 ký tự.');
      return;
    }

    try {
      await userService.resetPassword(resettingUser.maTaiKhoan, { matKhauMoi: newPassword });
      alert(`Đã đặt lại mật khẩu cho tài khoản "${resettingUser.tenDangNhap}" thành công!`);
      setResetModalOpen(false);
    } catch (err: any) {
      alert(err.message || 'Đặt lại mật khẩu thất bại.');
    }
  };

  const handleDeleteUser = async (user: User) => {
    if (user.maTaiKhoan === currentUser?.maTaiKhoan) {
      alert('Bạn không thể tự xóa tài khoản của chính mình!');
      return;
    }

    if (!window.confirm(`CẢNH BÁO: Bạn có chắc chắn muốn xóa tài khoản "${user.tenDangNhap}"? Thao tác này không thể hoàn tác.`)) {
      return;
    }

    try {
      await userService.deleteUser(user.maTaiKhoan);
      await fetchUsers();
    } catch (err: any) {
      alert(err.message || 'Xóa tài khoản thất bại.');
    }
  };

  return (
    <div className="user-management-page p-6 max-w-7xl mx-auto">
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-800">Quản lý Tài khoản & Phân quyền</h1>
          <p className="text-sm text-gray-500">
            Quản lý danh sách người dùng, vai trò bảo mật, trạng thái hoạt động và mật khẩu.
          </p>
        </div>

        {isAdmin && (
          <button
            onClick={handleOpenCreateModal}
            className="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg font-medium text-sm shadow-sm transition flex items-center gap-2 self-start md:self-auto"
          >
            <span>+ Tạo tài khoản mới</span>
          </button>
        )}
      </div>

      {/* Bộ lọc tìm kiếm */}
      <UserFilterBar onFilterChange={handleFilterChange} isLoading={loading} />

      {/* Thông báo lỗi */}
      {error && (
        <div className="mb-4 p-4 bg-red-50 border border-red-200 text-red-700 rounded-lg text-sm">
          {error}
        </div>
      )}

      {/* Bảng danh sách tài khoản */}
      <div className="bg-white rounded-lg shadow-sm border overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm border-collapse">
            <thead className="bg-gray-50 border-b text-gray-600 uppercase text-xs">
              <tr>
                <th className="py-3 px-4">Mã</th>
                <th className="py-3 px-4">Tài khoản / Họ tên</th>
                <th className="py-3 px-4">Liên hệ</th>
                <th className="py-3 px-4">Đơn vị / Chức vụ</th>
                <th className="py-3 px-4">Vai trò</th>
                <th className="py-3 px-4">Trạng thái</th>
                <th className="py-3 px-4 text-center">Thao tác</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200">
              {loading && users.length === 0 ? (
                <tr>
                  <td colSpan={7} className="py-8 text-center text-gray-500">
                    Đang tải dữ liệu...
                  </td>
                </tr>
              ) : users.length === 0 ? (
                <tr>
                  <td colSpan={7} className="py-8 text-center text-gray-500">
                    Không tìm thấy tài khoản nào phù hợp với bộ lọc.
                  </td>
                </tr>
              ) : (
                users.map((user) => (
                  <tr key={user.maTaiKhoan} className="hover:bg-gray-50 transition">
                    <td className="py-3 px-4 text-gray-500">#{user.maTaiKhoan}</td>
                    <td className="py-3 px-4">
                      <div className="font-semibold text-gray-800">{user.tenDangNhap}</div>
                      <div className="text-gray-600 text-xs">{user.hoTen}</div>
                      {user.cccd && (
                        <div className="text-gray-400 text-xs">CCCD: {user.cccd}</div>
                      )}
                    </td>
                    <td className="py-3 px-4">
                      {user.email && <div className="text-gray-700 text-xs">{user.email}</div>}
                      {user.soDienThoai && <div className="text-gray-500 text-xs">{user.soDienThoai}</div>}
                      {!user.email && !user.soDienThoai && <span className="text-gray-400 text-xs">-</span>}
                    </td>
                    <td className="py-3 px-4">
                      <div className="text-gray-800 text-xs">{user.donVi || '-'}</div>
                      <div className="text-gray-500 text-xs">{user.chucVu || ''}</div>
                    </td>
                    <td className="py-3 px-4">
                      <span
                        className={`inline-block px-2.5 py-0.5 rounded-full text-xs font-semibold ${
                          user.maVaiTro === 1
                            ? 'bg-purple-100 text-purple-800'
                            : user.maVaiTro === 2
                            ? 'bg-blue-100 text-blue-800'
                            : 'bg-emerald-100 text-emerald-800'
                        }`}
                      >
                        {user.tenVaiTro}
                      </span>
                    </td>
                    <td className="py-3 px-4">
                      <span
                        className={`inline-block px-2 py-0.5 rounded text-xs font-medium ${
                          user.trangThai === 'HoatDong'
                            ? 'bg-green-100 text-green-700'
                            : user.trangThai === 'KhoaTam'
                            ? 'bg-yellow-100 text-yellow-700'
                            : 'bg-red-100 text-red-700'
                        }`}
                      >
                        {user.trangThai === 'HoatDong'
                          ? 'Hoạt động'
                          : user.trangThai === 'KhoaTam'
                          ? 'Khóa tạm'
                          : 'Bị khóa'}
                      </span>
                    </td>
                    <td className="py-3 px-4 text-center">
                      <div className="flex items-center justify-center gap-1.5">
                        <button
                          onClick={() => handleOpenEditModal(user)}
                          className="px-2 py-1 text-xs bg-gray-100 hover:bg-gray-200 text-gray-700 rounded transition"
                          title="Sửa thông tin"
                        >
                          Sửa
                        </button>
                        <button
                          onClick={() => handleToggleStatus(user)}
                          className={`px-2 py-1 text-xs rounded transition ${
                            user.trangThai === 'HoatDong'
                              ? 'bg-amber-100 hover:bg-amber-200 text-amber-800'
                              : 'bg-emerald-100 hover:bg-emerald-200 text-emerald-800'
                          }`}
                          title={user.trangThai === 'HoatDong' ? 'Khóa tài khoản' : 'Mở khóa'}
                        >
                          {user.trangThai === 'HoatDong' ? 'Khóa' : 'Mở'}
                        </button>
                        <button
                          onClick={() => handleOpenResetModal(user)}
                          className="px-2 py-1 text-xs bg-blue-50 hover:bg-blue-100 text-blue-700 rounded transition"
                          title="Đặt lại mật khẩu"
                        >
                          Mật khẩu
                        </button>
                        {isAdmin && user.maTaiKhoan !== currentUser?.maTaiKhoan && (
                          <button
                            onClick={() => handleDeleteUser(user)}
                            className="px-2 py-1 text-xs bg-red-50 hover:bg-red-100 text-red-600 rounded transition"
                            title="Xóa tài khoản"
                          >
                            Xóa
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {/* Phân trang */}
        <div className="flex items-center justify-between p-4 border-t text-sm text-gray-600">
          <div>
            Hiển thị <strong>{users.length}</strong> / <strong>{totalCount}</strong> tài khoản
          </div>
          <div className="flex items-center gap-2">
            <button
              onClick={() => handlePageChange((filters.pageIndex || 1) - 1)}
              disabled={(filters.pageIndex || 1) <= 1 || loading}
              className="px-3 py-1 border rounded disabled:opacity-40 hover:bg-gray-50"
            >
              Trước
            </button>
            <span>
              Trang <strong>{filters.pageIndex || 1}</strong> / <strong>{totalPages}</strong>
            </span>
            <button
              onClick={() => handlePageChange((filters.pageIndex || 1) + 1)}
              disabled={(filters.pageIndex || 1) >= totalPages || loading}
              className="px-3 py-1 border rounded disabled:opacity-40 hover:bg-gray-50"
            >
              Sau
            </button>
          </div>
        </div>
      </div>

      {/* Modal Thêm mới / Cập nhật */}
      <UserModal
        isOpen={modalOpen}
        onClose={() => setModalOpen(false)}
        onSubmit={handleModalSubmit}
        initialData={selectedUser}
        mode={modalMode}
      />

      {/* Modal Đặt lại mật khẩu */}
      {resetModalOpen && (
        <div className="fixed inset-0 z-50 bg-black bg-opacity-50 flex items-center justify-center p-4">
          <div className="bg-white rounded-lg max-w-sm w-full p-6 shadow-xl">
            <h3 className="text-lg font-bold mb-2 text-gray-800">
              Đặt lại mật khẩu cho {resettingUser?.tenDangNhap}
            </h3>
            <p className="text-xs text-gray-500 mb-4">
              Nhập mật khẩu mới cho người dùng này. Mật khẩu phải có tối thiểu 6 ký tự.
            </p>
            <form onSubmit={handleConfirmResetPassword} className="space-y-4">
              <div>
                <input
                  type="password"
                  required
                  minLength={6}
                  value={newPassword}
                  onChange={(e) => setNewPassword(e.target.value)}
                  placeholder="Nhập mật khẩu mới..."
                  className="w-full px-3 py-2 border rounded text-sm focus:ring-2 focus:ring-blue-500"
                />
              </div>
              <div className="flex justify-end gap-2">
                <button
                  type="button"
                  onClick={() => setResetModalOpen(false)}
                  className="px-3 py-1.5 border rounded text-sm text-gray-600 hover:bg-gray-100"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  className="px-4 py-1.5 bg-blue-600 hover:bg-blue-700 text-white rounded text-sm font-medium"
                >
                  Xác nhận đổi
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export default UserManagement;
