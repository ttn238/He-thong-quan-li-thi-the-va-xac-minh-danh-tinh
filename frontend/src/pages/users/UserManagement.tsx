import React, { useState, useEffect, useCallback } from 'react';
import { User, UserFilterParams, CreateUserPayload, UpdateUserPayload, SYSTEM_ROLES } from '../../types/User';
import userService from '../../services/userService';
import roleService from '../../services/roleService';
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

  // Quick Role Assignment State
  const [roleModalOpen, setRoleModalOpen] = useState(false);
  const [roleAssigningUser, setRoleAssigningUser] = useState<User | null>(null);
  const [newSelectedRoleId, setNewSelectedRoleId] = useState<number>(2);
  const [roleSubmitting, setRoleSubmitting] = useState(false);

  // Reset Password State
  const [resetModalOpen, setResetModalOpen] = useState(false);
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [resettingUser, setResettingUser] = useState<User | null>(null);
  const [resetSubmitting, setResetSubmitting] = useState(false);

  const currentUser = authHelper.getCurrentUser();
  const isAdmin = authHelper.isAdmin();

  const fetchUsers = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await userService.getUsers(filters);
      setUsers(res.items || []);
      setTotalCount(res.totalCount || 0);
      setTotalPages(res.totalPages || Math.ceil((res.totalCount || 1) / (filters.pageSize || 10)));
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
    setConfirmPassword('');
    setResetModalOpen(true);
  };

  const handleConfirmResetPassword = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!resettingUser) return;
    if (!newPassword || newPassword.length < 6) {
      alert('Mật khẩu mới phải có tối thiểu 6 ký tự.');
      return;
    }
    if (newPassword !== confirmPassword) {
      alert('Mật khẩu xác nhận không khớp với mật khẩu mới.');
      return;
    }

    setResetSubmitting(true);
    try {
      await userService.resetPassword(resettingUser.maTaiKhoan, { matKhauMoi: newPassword });
      alert(`Đã đặt lại mật khẩu cho tài khoản "${resettingUser.tenDangNhap}" thành công!`);
      setResetModalOpen(false);
    } catch (err: any) {
      alert(err.message || 'Đặt lại mật khẩu thất bại.');
    } finally {
      setResetSubmitting(false);
    }
  };

  const handleDeleteUser = async (user: User) => {
    if (user.maTaiKhoan === currentUser?.maTaiKhoan) {
      alert('Bạn không thể tự xóa tài khoản của chính mình!');
      return;
    }

    if (!window.confirm(`CẢNH BÁO: Bạn có chắc chắn muốn xóa tài khoản "${user.tenDangNhap}"? Thao tác này sẽ chuyển tài khoản sang trạng thái Bị khóa vĩnh viễn.`)) {
      return;
    }

    try {
      await userService.deleteUser(user.maTaiKhoan);
      await fetchUsers();
    } catch (err: any) {
      alert(err.message || 'Xóa tài khoản thất bại.');
    }
  };

  const handleOpenRoleModal = (user: User) => {
    setRoleAssigningUser(user);
    setNewSelectedRoleId(user.maVaiTro || 2);
    setRoleModalOpen(true);
  };

  const handleAssignRoleSubmit = async () => {
    if (!roleAssigningUser) return;

    if (roleAssigningUser.maTaiKhoan === currentUser?.maTaiKhoan && roleAssigningUser.maVaiTro === 1 && newSelectedRoleId !== 1) {
      alert('Quy tắc an toàn: Quản trị viên không thể tự hạ quyền của chính mình!');
      return;
    }

    setRoleSubmitting(true);
    try {
      const res = await roleService.assignRole({
        maTaiKhoan: roleAssigningUser.maTaiKhoan,
        maVaiTro: newSelectedRoleId,
      });
      alert(res.message || 'Đã cập nhật vai trò thành công!');
      setRoleModalOpen(false);
      await fetchUsers();
    } catch (err: any) {
      alert(err.message || 'Gán vai trò thất bại.');
    } finally {
      setRoleSubmitting(false);
    }
  };

  const getRoleBadge = (user: User) => {
    const roleDef = SYSTEM_ROLES.find((r) => r.id === user.maVaiTro);
    const color = roleDef?.badgeColor || 'bg-gray-100 text-gray-800 border-gray-200';
    const label = user.tenVaiTro || roleDef?.label || 'Không xác định';

    return (
      <span className={`inline-block px-2.5 py-0.5 rounded-full text-xs font-semibold border ${color}`}>
        {label}
      </span>
    );
  };

  const getStatusBadge = (status: string) => {
    switch (status) {
      case 'HoatDong':
        return <span className="inline-block px-2 py-0.5 rounded text-xs font-medium bg-green-100 text-green-700">Hoạt động</span>;
      case 'KhoaTam':
        return <span className="inline-block px-2 py-0.5 rounded text-xs font-medium bg-yellow-100 text-yellow-700">Khóa tạm</span>;
      default:
        return <span className="inline-block px-2 py-0.5 rounded text-xs font-medium bg-red-100 text-red-700">Bị khóa</span>;
    }
  };

  return (
    <div className="user-management p-6 bg-gray-50 min-h-screen">
      <div className="max-w-7xl mx-auto">
        {/* Header */}
        <div className="flex flex-col md:flex-row md:items-center md:justify-between mb-6 pb-4 border-b">
          <div>
            <h1 className="text-2xl font-bold text-gray-900">Quản lý Tài khoản & Phân quyền</h1>
            <p className="text-sm text-gray-500 mt-1">
              Phân hệ 4.4: Quản lý danh tính, phân quyền 5 nhóm vai trò và bảo vệ an toàn thông tin
            </p>
          </div>
          {isAdmin && (
            <div className="mt-4 md:mt-0 flex items-center gap-2">
              <a
                href="/roles"
                className="inline-flex items-center px-4 py-2 bg-slate-800 text-white rounded-md text-sm font-medium hover:bg-slate-900 shadow-sm transition gap-1.5"
              >
                <span>🔑</span> Quản lý 5 Vai trò (RBAC)
              </a>
              <button
                onClick={handleOpenCreateModal}
                className="inline-flex items-center px-4 py-2 bg-blue-600 text-white rounded-md text-sm font-medium hover:bg-blue-700 shadow-sm transition"
              >
                + Tạo tài khoản mới
              </button>
            </div>
          )}
        </div>

        {/* Filter Bar */}
        <UserFilterBar onFilterChange={handleFilterChange} isLoading={loading} />

        {/* Error message */}
        {error && (
          <div className="mb-4 p-4 bg-red-50 border border-red-200 text-red-700 rounded-md text-sm">
            {error}
          </div>
        )}

        {/* Users Table */}
        <div className="bg-white rounded-lg shadow border overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-left border-collapse text-sm">
              <thead className="bg-gray-100 text-gray-700 uppercase font-semibold text-xs border-b">
                <tr>
                  <th className="py-3 px-4">ID</th>
                  <th className="py-3 px-4">Tài khoản & Họ tên</th>
                  <th className="py-3 px-4">Thông tin liên hệ</th>
                  <th className="py-3 px-4">Đơn vị & Chức vụ</th>
                  <th className="py-3 px-4">Vai trò (Role)</th>
                  <th className="py-3 px-4">Trạng thái</th>
                  <th className="py-3 px-4 text-center">Thao tác</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-200">
                {loading ? (
                  <tr>
                    <td colSpan={7} className="py-8 text-center text-gray-500">
                      Đang tải danh sách tài khoản...
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
                      <td className="py-3 px-4 text-gray-500 font-mono">#{user.maTaiKhoan}</td>
                      <td className="py-3 px-4">
                        <div className="font-semibold text-gray-900">{user.tenDangNhap}</div>
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
                        {getRoleBadge(user)}
                      </td>
                      <td className="py-3 px-4">
                        {getStatusBadge(user.trangThai)}
                      </td>
                      <td className="py-3 px-4 text-center">
                        <div className="flex items-center justify-center gap-1.5">
                          <button
                            onClick={() => handleOpenEditModal(user)}
                            className="px-2.5 py-1 text-xs bg-gray-100 hover:bg-gray-200 text-gray-700 rounded transition font-medium"
                            title="Sửa thông tin"
                          >
                            Sửa
                          </button>
                          {isAdmin && (
                            <button
                              onClick={() => handleOpenRoleModal(user)}
                              className="px-2.5 py-1 text-xs bg-purple-50 hover:bg-purple-100 text-purple-700 rounded transition font-medium"
                              title="Gán / Đổi vai trò"
                            >
                              Đổi Role
                            </button>
                          )}
                          <button
                            onClick={() => handleToggleStatus(user)}
                            className={`px-2.5 py-1 text-xs rounded transition font-medium ${
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
                            className="px-2.5 py-1 text-xs bg-blue-50 hover:bg-blue-100 text-blue-700 rounded transition font-medium"
                            title="Đặt lại mật khẩu"
                          >
                            Đổi MK
                          </button>
                          {isAdmin && user.maTaiKhoan !== currentUser?.maTaiKhoan && (
                            <button
                              onClick={() => handleDeleteUser(user)}
                              className="px-2.5 py-1 text-xs bg-red-50 hover:bg-red-100 text-red-600 rounded transition font-medium"
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

          {/* Pagination Bar */}
          <div className="flex flex-col sm:flex-row items-center justify-between px-4 py-3 bg-gray-50 border-t">
            <div className="text-xs text-gray-500 mb-2 sm:mb-0">
              Tổng số <span className="font-semibold text-gray-700">{totalCount}</span> tài khoản | Trang{' '}
              <span className="font-semibold text-gray-700">{filters.pageIndex}</span> / {totalPages}
            </div>
            <div className="flex gap-1">
              <button
                onClick={() => handlePageChange((filters.pageIndex || 1) - 1)}
                disabled={(filters.pageIndex || 1) <= 1 || loading}
                className="px-3 py-1 border rounded text-xs disabled:opacity-50 hover:bg-gray-100 transition"
              >
                Trang trước
              </button>
              <button
                onClick={() => handlePageChange((filters.pageIndex || 1) + 1)}
                disabled={(filters.pageIndex || 1) >= totalPages || loading}
                className="px-3 py-1 border rounded text-xs disabled:opacity-50 hover:bg-gray-100 transition"
              >
                Trang tiếp
              </button>
            </div>
          </div>
        </div>
      </div>

      {/* Modal Thêm / Sửa tài khoản */}
      <UserModal
        isOpen={modalOpen}
        onClose={() => setModalOpen(false)}
        onSubmit={handleModalSubmit}
        initialData={selectedUser}
        mode={modalMode}
      />

      {/* Modal Đặt lại mật khẩu */}
      {resetModalOpen && resettingUser && (
        <div className="fixed inset-0 z-50 overflow-y-auto bg-black bg-opacity-50 flex items-center justify-center p-4">
          <div className="bg-white rounded-lg max-w-md w-full p-6 shadow-xl">
            <h3 className="text-lg font-bold text-gray-900 mb-2">
              Đặt lại mật khẩu cho tài khoản
            </h3>
            <p className="text-sm text-gray-600 mb-4 font-mono">
              Tài khoản: <span className="font-semibold text-blue-600">{resettingUser.tenDangNhap}</span> ({resettingUser.hoTen})
            </p>

            <form onSubmit={handleConfirmResetPassword} className="space-y-4">
              <div>
                <label className="block text-xs font-semibold text-gray-700 uppercase mb-1">
                  Mật khẩu mới <span className="text-red-500">*</span>
                </label>
                <input
                  type="password"
                  required
                  placeholder="Tối thiểu 6 ký tự"
                  className="w-full px-3 py-2 border rounded-md text-sm focus:ring-2 focus:ring-blue-500"
                  value={newPassword}
                  onChange={(e) => setNewPassword(e.target.value)}
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-gray-700 uppercase mb-1">
                  Xác nhận mật khẩu mới <span className="text-red-500">*</span>
                </label>
                <input
                  type="password"
                  required
                  placeholder="Nhập lại mật khẩu mới"
                  className="w-full px-3 py-2 border rounded-md text-sm focus:ring-2 focus:ring-blue-500"
                  value={confirmPassword}
                  onChange={(e) => setConfirmPassword(e.target.value)}
                />
              </div>

              <div className="flex justify-end gap-2 pt-2">
                <button
                  type="button"
                  onClick={() => setResetModalOpen(false)}
                  className="px-4 py-2 border rounded-md text-sm text-gray-700 hover:bg-gray-100"
                  disabled={resetSubmitting}
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  className="px-4 py-2 bg-blue-600 text-white rounded-md text-sm font-medium hover:bg-blue-700 disabled:opacity-50"
                  disabled={resetSubmitting}
                >
                  {resetSubmitting ? 'Đang cập nhật...' : 'Xác nhận đổi mật khẩu'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Modal Gán / Đổi Vai Trò Nhanh */}
      {roleModalOpen && roleAssigningUser && (
        <div className="fixed inset-0 z-50 overflow-y-auto bg-black bg-opacity-50 flex items-center justify-center p-4">
          <div className="bg-white rounded-lg max-w-md w-full p-6 shadow-xl">
            <h3 className="text-lg font-bold text-gray-900 mb-2">
              Gán / Đổi vai trò tài khoản
            </h3>
            <p className="text-sm text-gray-600 mb-4 font-mono">
              Tài khoản: <span className="font-semibold text-blue-600">{roleAssigningUser.tenDangNhap}</span> ({roleAssigningUser.hoTen})
            </p>

            <div className="space-y-4">
              <div>
                <label className="block text-xs font-semibold text-gray-700 uppercase mb-1">
                  Vai trò mới cần gán <span className="text-red-500">*</span>
                </label>
                <select
                  value={newSelectedRoleId}
                  onChange={(e) => setNewSelectedRoleId(Number(e.target.value))}
                  className="w-full px-3 py-2 border rounded-md text-sm bg-white focus:ring-2 focus:ring-purple-500"
                >
                  {SYSTEM_ROLES.map((r) => (
                    <option key={r.id} value={r.id}>
                      [{r.code}] - {r.label}
                    </option>
                  ))}
                </select>
              </div>

              {roleAssigningUser.maTaiKhoan === currentUser?.maTaiKhoan && roleAssigningUser.maVaiTro === 1 && (
                <div className="p-3 bg-amber-50 border border-amber-200 text-amber-800 text-xs rounded-md">
                  ⚠️ Lưu ý: Bạn đang thao tác trên tài khoản của chính mình. Quy tắc an ninh không cho phép Quản trị viên tự hạ quyền thành tài khoản thường.
                </div>
              )}

              <div className="flex justify-end gap-2 pt-2">
                <button
                  type="button"
                  onClick={() => setRoleModalOpen(false)}
                  className="px-4 py-2 border rounded-md text-sm text-gray-700 hover:bg-gray-100"
                  disabled={roleSubmitting}
                >
                  Hủy
                </button>
                <button
                  type="button"
                  onClick={handleAssignRoleSubmit}
                  className="px-4 py-2 bg-purple-600 text-white rounded-md text-sm font-medium hover:bg-purple-700 disabled:opacity-50"
                  disabled={roleSubmitting}
                >
                  {roleSubmitting ? 'Đang xử lý...' : 'Xác nhận gán vai trò'}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default UserManagement;
