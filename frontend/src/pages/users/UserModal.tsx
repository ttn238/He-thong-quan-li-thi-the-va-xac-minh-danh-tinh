import React, { useState, useEffect } from 'react';
import { User, CreateUserPayload, UpdateUserPayload, SYSTEM_ROLES } from '../../types/User';

interface UserModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (userData: CreateUserPayload | UpdateUserPayload) => Promise<void>;
  initialData?: User | null;
  mode: 'create' | 'edit';
}

export const UserModal: React.FC<UserModalProps> = ({
  isOpen,
  onClose,
  onSubmit,
  initialData,
  mode,
}) => {
  const [formData, setFormData] = useState({
    tenDangNhap: '',
    matKhau: '',
    hoTen: '',
    email: '',
    soDienThoai: '',
    cccd: '',
    donVi: '',
    chucVu: '',
    maVaiTro: 5, // Default to NGUOI_DAN
  });

  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    if (mode === 'edit' && initialData) {
      setFormData({
        tenDangNhap: initialData.tenDangNhap,
        matKhau: '',
        hoTen: initialData.hoTen,
        email: initialData.email || '',
        soDienThoai: initialData.soDienThoai || '',
        cccd: initialData.cccd || '',
        donVi: initialData.donVi || '',
        chucVu: initialData.chucVu || '',
        maVaiTro: initialData.maVaiTro,
      });
    } else {
      setFormData({
        tenDangNhap: '',
        matKhau: '',
        hoTen: '',
        email: '',
        soDienThoai: '',
        cccd: '',
        donVi: '',
        chucVu: '',
        maVaiTro: 5,
      });
    }
    setError(null);
  }, [isOpen, initialData, mode]);

  if (!isOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (mode === 'create') {
      if (!formData.tenDangNhap.trim()) {
        setError('Vui lòng nhập tên đăng nhập.');
        return;
      }
      if (!/^[a-zA-Z0-9_]{3,50}$/.test(formData.tenDangNhap.trim())) {
        setError('Tên đăng nhập chỉ chứa chữ cái, số, dấu gạch dưới (3 - 50 ký tự).');
        return;
      }
      if (!formData.matKhau || formData.matKhau.length < 6) {
        setError('Mật khẩu bắt buộc và phải có ít nhất 6 ký tự.');
        return;
      }
    }

    if (!formData.hoTen.trim()) {
      setError('Vui lòng nhập họ và tên.');
      return;
    }

    if (!formData.email?.trim() && !formData.soDienThoai?.trim()) {
      setError('Vui lòng cung cấp ít nhất một hình thức liên lạc (Email hoặc Số điện thoại).');
      return;
    }

    if (formData.cccd?.trim() && !/^[0-9]{9,12}$/.test(formData.cccd.trim())) {
      setError('Số CCCD/Định danh phải gồm từ 9 đến 12 chữ số.');
      return;
    }

    if (formData.soDienThoai?.trim() && !/^[0-9+]{9,15}$/.test(formData.soDienThoai.trim())) {
      setError('Số điện thoại không hợp lệ (9 đến 15 chữ số).');
      return;
    }

    setSubmitting(true);
    try {
      if (mode === 'create') {
        await onSubmit({
          tenDangNhap: formData.tenDangNhap.trim(),
          matKhau: formData.matKhau,
          hoTen: formData.hoTen.trim(),
          email: formData.email.trim() || undefined,
          soDienThoai: formData.soDienThoai.trim() || undefined,
          cccd: formData.cccd.trim() || undefined,
          donVi: formData.donVi.trim() || undefined,
          chucVu: formData.chucVu.trim() || undefined,
          maVaiTro: Number(formData.maVaiTro),
        });
      } else {
        await onSubmit({
          hoTen: formData.hoTen.trim(),
          email: formData.email.trim() || undefined,
          soDienThoai: formData.soDienThoai.trim() || undefined,
          cccd: formData.cccd.trim() || undefined,
          donVi: formData.donVi.trim() || undefined,
          chucVu: formData.chucVu.trim() || undefined,
          maVaiTro: Number(formData.maVaiTro),
        });
      }
      onClose();
    } catch (err: any) {
      setError(err.message || 'Có lỗi xảy ra khi lưu thông tin tài khoản.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto bg-black bg-opacity-50 flex items-center justify-center p-4">
      <div className="bg-white rounded-lg max-w-lg w-full p-6 shadow-xl relative">
        <h3 className="text-xl font-bold mb-4 text-gray-800">
          {mode === 'create' ? 'Tạo mới tài khoản người dùng' : `Chỉnh sửa tài khoản: ${initialData?.tenDangNhap}`}
        </h3>

        {error && (
          <div className="mb-4 p-3 bg-red-100 border border-red-300 text-red-700 text-sm rounded">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          {mode === 'create' && (
            <>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Tên đăng nhập <span className="text-red-500">*</span>
                </label>
                <input
                  type="text"
                  required
                  className="w-full px-3 py-2 border rounded-md text-sm focus:ring-2 focus:ring-blue-500"
                  value={formData.tenDangNhap}
                  onChange={(e) => setFormData({ ...formData, tenDangNhap: e.target.value })}
                  placeholder="canbo_dieu_tra"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Mật khẩu khởi tạo <span className="text-red-500">*</span>
                </label>
                <input
                  type="password"
                  required
                  className="w-full px-3 py-2 border rounded-md text-sm focus:ring-2 focus:ring-blue-500"
                  value={formData.matKhau}
                  onChange={(e) => setFormData({ ...formData, matKhau: e.target.value })}
                  placeholder="Tối thiểu 6 ký tự"
                />
              </div>
            </>
          )}

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Họ và tên <span className="text-red-500">*</span>
            </label>
            <input
              type="text"
              required
              className="w-full px-3 py-2 border rounded-md text-sm focus:ring-2 focus:ring-blue-500"
              value={formData.hoTen}
              onChange={(e) => setFormData({ ...formData, hoTen: e.target.value })}
              placeholder="Nguyễn Văn A"
            />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Email</label>
              <input
                type="email"
                className="w-full px-3 py-2 border rounded-md text-sm focus:ring-2 focus:ring-blue-500"
                value={formData.email}
                onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                placeholder="email@domain.com"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Số điện thoại</label>
              <input
                type="tel"
                className="w-full px-3 py-2 border rounded-md text-sm focus:ring-2 focus:ring-blue-500"
                value={formData.soDienThoai}
                onChange={(e) => setFormData({ ...formData, soDienThoai: e.target.value })}
                placeholder="0912345678"
              />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Số CCCD / Định danh</label>
              <input
                type="text"
                className="w-full px-3 py-2 border rounded-md text-sm focus:ring-2 focus:ring-blue-500"
                value={formData.cccd}
                onChange={(e) => setFormData({ ...formData, cccd: e.target.value })}
                placeholder="12 chữ số"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Vai trò hệ thống <span className="text-red-500">*</span>
              </label>
              <select
                className="w-full px-3 py-2 border rounded-md text-sm focus:ring-2 focus:ring-blue-500"
                value={formData.maVaiTro}
                onChange={(e) => setFormData({ ...formData, maVaiTro: Number(e.target.value) })}
              >
                {SYSTEM_ROLES.map((role) => (
                  <option key={role.id} value={role.id}>
                    {role.label} ({role.code})
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Đơn vị công tác</label>
              <input
                type="text"
                className="w-full px-3 py-2 border rounded-md text-sm focus:ring-2 focus:ring-blue-500"
                value={formData.donVi}
                onChange={(e) => setFormData({ ...formData, donVi: e.target.value })}
                placeholder="Công an Q1, Nhà xác TT..."
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Chức vụ</label>
              <input
                type="text"
                className="w-full px-3 py-2 border rounded-md text-sm focus:ring-2 focus:ring-blue-500"
                value={formData.chucVu}
                onChange={(e) => setFormData({ ...formData, chucVu: e.target.value })}
                placeholder="Cán bộ điều tra..."
              />
            </div>
          </div>

          <div className="flex justify-end gap-3 mt-6">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 border rounded-md text-sm text-gray-700 hover:bg-gray-100"
              disabled={submitting}
            >
              Hủy bỏ
            </button>
            <button
              type="submit"
              className="px-4 py-2 bg-blue-600 text-white rounded-md text-sm hover:bg-blue-700"
              disabled={submitting}
            >
              {submitting ? 'Đang lưu...' : mode === 'create' ? 'Tạo mới' : 'Cập nhật'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default UserModal;
