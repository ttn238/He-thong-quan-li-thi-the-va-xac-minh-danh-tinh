import React, { useState, useEffect } from 'react';
import { StorageLocation, CreateStorageLocationPayload, UpdateStorageLocationPayload } from '../../types/StorageLocation';
import storageService from '../../services/storageService';

interface StorageModalProps {
  isOpen: boolean;
  mode: 'create' | 'edit';
  initialData?: StorageLocation | null;
  onClose: () => void;
  onSuccess: () => void;
}

export const StorageModal: React.FC<StorageModalProps> = ({
  isOpen,
  mode,
  initialData,
  onClose,
  onSuccess,
}) => {
  const [khu, setKhu] = useState('');
  const [tu, setTu] = useState('');
  const [ngan, setNgan] = useState('');
  const [sucChua, setSucChua] = useState(1);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (mode === 'edit' && initialData) {
      setKhu(initialData.khu || '');
      setTu(initialData.tu || '');
      setNgan(initialData.ngan || '');
      setSucChua(initialData.sucChua || 1);
    } else {
      setKhu('');
      setTu('');
      setNgan('');
      setSucChua(1);
    }
    setError(null);
  }, [isOpen, mode, initialData]);

  if (!isOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    const cleanKhu = khu.trim();
    const cleanTu = tu.trim();
    const cleanNgan = ngan.trim();

    if (!cleanKhu) {
      setError('Vui lòng nhập tên Khu vực lưu trữ.');
      return;
    }
    if (!cleanTu) {
      setError('Vui lòng nhập tên Tủ lưu trữ.');
      return;
    }
    if (!cleanNgan) {
      setError('Vui lòng nhập tên Ngăn lưu trữ.');
      return;
    }
    if (sucChua < 1) {
      setError('Sức chứa tối thiểu phải là 1.');
      return;
    }

    setSubmitting(true);
    try {
      if (mode === 'create') {
        const payload: CreateStorageLocationPayload = {
          khu: cleanKhu,
          tu: cleanTu,
          ngan: cleanNgan,
          sucChua,
        };
        await storageService.createLocation(payload);
      } else if (mode === 'edit' && initialData) {
        const payload: UpdateStorageLocationPayload = {
          khu: cleanKhu,
          tu: cleanTu,
          ngan: cleanNgan,
          sucChua,
        };
        await storageService.updateLocation(initialData.maViTri, payload);
      }
      onSuccess();
      onClose();
    } catch (err: any) {
      setError(err.message || 'Thao tác không thành công.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-xs p-4 overflow-y-auto">
      <div className="bg-white rounded-xl shadow-2xl w-full max-w-md overflow-hidden border border-slate-100">
        {/* Header */}
        <div className="bg-slate-900 text-white px-6 py-4 flex items-center justify-between">
          <div>
            <h3 className="font-bold text-lg text-white">
              {mode === 'create' ? 'THÊM MỚI VỊ TRÍ LƯU TRỮ' : `CHỈNH SỬA VỊ TRÍ #${initialData?.maViTri}`}
            </h3>
            <p className="text-xs text-amber-300 mt-0.5">
              Bộ ba (Khu, Tủ, Ngăn) là định danh vật lý duy nhất trong kho
            </p>
          </div>
          <button
            onClick={onClose}
            className="text-slate-400 hover:text-white text-xl font-bold p-1 rounded-sm"
          >
            ✕
          </button>
        </div>

        {/* Form Body */}
        <form onSubmit={handleSubmit} className="p-6 space-y-4">
          {error && (
            <div className="bg-red-50 border border-red-200 text-red-700 text-sm p-3 rounded-lg">
              <span>{error}</span>
            </div>
          )}

          <div>
            <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
              Khu vực <span className="text-red-500">*</span>
            </label>
            <input
              type="text"
              value={khu}
              onChange={(e) => setKhu(e.target.value)}
              placeholder="Ví dụ: Khu A, Khu B, Kho lạnh 1..."
              maxLength={50}
              className="w-full px-3 py-2 text-sm border border-slate-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none transition"
              required
            />
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
              Tủ lưu trữ <span className="text-red-500">*</span>
            </label>
            <input
              type="text"
              value={tu}
              onChange={(e) => setTu(e.target.value)}
              placeholder="Ví dụ: A01, A02, T01..."
              maxLength={50}
              className="w-full px-3 py-2 text-sm border border-slate-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none transition"
              required
            />
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
              Ngăn chứa <span className="text-red-500">*</span>
            </label>
            <input
              type="text"
              value={ngan}
              onChange={(e) => setNgan(e.target.value)}
              placeholder="Ví dụ: N01, N02, Ngăn 1..."
              maxLength={50}
              className="w-full px-3 py-2 text-sm border border-slate-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none transition"
              required
            />
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
              Sức chứa tối đa (người) <span className="text-red-500">*</span>
            </label>
            <input
              type="number"
              value={sucChua}
              min={1}
              max={100}
              onChange={(e) => setSucChua(Math.max(1, parseInt(e.target.value) || 1))}
              className="w-full px-3 py-2 text-sm border border-slate-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none transition"
              required
            />
            <p className="text-xs text-slate-500 mt-1">Thông thường mỗi ngăn chứa tối đa 1 thi thể.</p>
          </div>

          {/* Action Buttons */}
          <div className="flex items-center justify-end gap-3 pt-3 border-t border-slate-100 mt-6">
            <button
              type="button"
              onClick={onClose}
              disabled={submitting}
              className="px-4 py-2 text-sm font-medium text-slate-700 bg-slate-100 hover:bg-slate-200 rounded-lg transition"
            >
              Hủy
            </button>
            <button
              type="submit"
              disabled={submitting}
              className="px-5 py-2 text-sm font-semibold text-white bg-slate-900 hover:bg-slate-800 rounded-lg shadow-sm transition flex items-center gap-2"
            >
              {submitting ? 'Đang lưu...' : mode === 'create' ? 'Tạo vị trí' : 'Lưu cập nhật'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default StorageModal;
