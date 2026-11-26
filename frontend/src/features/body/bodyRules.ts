import authHelper from '../auth/authHelper';
import type { BodyGender, BodyRecord, UpdateBodyPayload } from '../../types/Body';

/** Các trường hệ thống/nghiệp vụ cố định, không cho phép sửa trực tiếp trong form cập nhật. */
export const BODY_IMMUTABLE_FIELDS = [
  { key: 'maHoSoThiThe', label: 'Mã hồ sơ thi thể', reason: 'Định danh duy nhất do hệ thống tự sinh.' },
  { key: 'trangThai', label: 'Trạng thái hồ sơ', reason: 'Chỉ thay đổi qua luồng nghiệp vụ xác minh, bàn giao hoặc đóng/mở hồ sơ.' },
  { key: 'maTinBao', label: 'Mã tin báo liên kết', reason: 'Giữ nguyên liên kết nguồn tin ban đầu để phục vụ truy vết.' },
  { key: 'viTriLuuTru', label: 'Vị trí lưu trữ (Khu/Tủ/Ngăn)', reason: 'Cập nhật qua chức năng quản lý kho lưu trữ nhà xác.' },
] as const;

/** Các trường nhạy cảm bị hạn chế chỉnh sửa đối với phạm vi PARTIAL (Nhân viên nhà xác). */
export const BODY_PARTIAL_RESTRICTED_FIELDS = ['thoiGianPhatHien', 'diaDiem', 'chieuCao', 'dacDiemCoThe'] as const;

export interface BodyEditFormValues {
  thoiGianPhatHien: string;
  diaDiem: string;
  gioiTinh: BodyGender;
  tuoiUocLuong: string;
  chieuCao: string;
  dacDiemCoThe: string;
}

export type BodyFormErrors = Partial<Record<keyof BodyEditFormValues, string>>;

/** Kiểm tra tài khoản hiện tại có quyền cập nhật hồ sơ thi thể (BODY:UPDATE) hay không. */
export function canEditBody(): boolean {
  return authHelper.isOfficer() || authHelper.isApprover() || authHelper.isMorgue();
}

/** Tài khoản nhà xác chỉ có quyền cập nhật phạm vi PARTIAL (không sửa địa điểm, chiều cao, đặc điểm cơ thể). */
export function isRestrictedBodyEditor(): boolean {
  return authHelper.isMorgue() && !authHelper.isOfficer() && !authHelper.isApprover();
}

/** Hồ sơ đã đóng (DongHoSo) không cho phép chỉnh sửa cho đến khi được mở lại. */
export function isBodyClosed(body: BodyRecord | null | undefined): boolean {
  return body?.trangThai === 'DongHoSo';
}

export function toDateTimeLocalValue(iso?: string | null): string {
  if (!iso) return '';
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return '';
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

export function formatDateTimeDisplay(iso?: string | null): string {
  if (!iso) return '—';
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return iso;
  return date.toLocaleString('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

export function mapBodyToFormValues(body: BodyRecord): BodyEditFormValues {
  const validGenders: BodyGender[] = ['Nam', 'Nu', 'Khac', 'KhongXacDinh'];
  const gioiTinh = validGenders.includes(body.gioiTinh as BodyGender)
    ? (body.gioiTinh as BodyGender)
    : 'KhongXacDinh';

  return {
    thoiGianPhatHien: toDateTimeLocalValue(body.thoiGianPhatHien),
    diaDiem: body.diaDiem ?? '',
    gioiTinh,
    tuoiUocLuong: body.tuoiUocLuong != null ? String(body.tuoiUocLuong) : '',
    chieuCao: body.chieuCao != null ? String(body.chieuCao) : '',
    dacDiemCoThe: body.dacDiemCoThe ?? '',
  };
}

export function validateBodyForm(
  values: BodyEditFormValues,
  restricted: boolean,
): BodyFormErrors {
  const errors: BodyFormErrors = {};

  if (!restricted) {
    if (!values.thoiGianPhatHien) {
      errors.thoiGianPhatHien = 'Thời gian phát hiện không được để trống.';
    } else {
      const dt = new Date(values.thoiGianPhatHien);
      if (Number.isNaN(dt.getTime())) {
        errors.thoiGianPhatHien = 'Thời gian phát hiện không hợp lệ.';
      } else if (dt.getTime() > Date.now()) {
        errors.thoiGianPhatHien = 'Thời gian phát hiện không được ở tương lai.';
      }
    }
  }

  if (!['Nam', 'Nu', 'Khac', 'KhongXacDinh'].includes(values.gioiTinh)) {
    errors.gioiTinh = 'Giới tính không hợp lệ.';
  }

  if (values.tuoiUocLuong.trim() !== '') {
    const age = Number(values.tuoiUocLuong);
    if (!Number.isInteger(age) || age < 0 || age > 130) {
      errors.tuoiUocLuong = 'Tuổi ước lượng phải là số nguyên từ 0 đến 130.';
    }
  }

  if (!restricted) {
    const diaDiem = values.diaDiem.trim();
    if (!diaDiem) {
      errors.diaDiem = 'Địa điểm phát hiện không được để trống.';
    } else if (diaDiem.length > 255) {
      errors.diaDiem = 'Địa điểm phát hiện không được vượt quá 255 ký tự.';
    }

    if (values.chieuCao.trim() !== '') {
      const height = Number(values.chieuCao);
      if (Number.isNaN(height) || height <= 0 || height > 300) {
        errors.chieuCao = 'Chiều cao phải lớn hơn 0 và không vượt quá 300 cm.';
      }
    }
  }

  return errors;
}

export function buildUpdatePayload(
  values: BodyEditFormValues,
  restricted: boolean,
): UpdateBodyPayload {
  const payload: UpdateBodyPayload = {
    gioiTinh: values.gioiTinh,
  };

  if (values.tuoiUocLuong.trim() !== '') {
    payload.tuoiUocLuong = Number(values.tuoiUocLuong);
  }

  if (!restricted) {
    payload.thoiGianPhatHien = values.thoiGianPhatHien;
    payload.diaDiem = values.diaDiem.trim();
    if (values.chieuCao.trim() !== '') {
      payload.chieuCao = Number(values.chieuCao);
    }
    payload.dacDiemCoThe = values.dacDiemCoThe.trim();
  }

  return payload;
}
