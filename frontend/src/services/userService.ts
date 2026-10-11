import ApiClient from './api';
import {
  User,
  PagedResult,
  UserFilterParams,
  CreateUserPayload,
  UpdateUserPayload,
  UpdateProfilePayload,
  ChangeStatusPayload,
  ResetPasswordPayload,
} from '../types/User';

export const userService = {
  /**
   * Lấy danh sách tài khoản có phân trang, lọc và tìm kiếm (Chỉ Quản trị viên)
   */
  getUsers(params?: UserFilterParams): Promise<PagedResult<User>> {
    return ApiClient.get<PagedResult<User>>('/users', params);
  },

  /**
   * Lấy thông tin chi tiết một tài khoản theo ID (Chỉ Quản trị viên)
   */
  getUserById(id: number): Promise<User> {
    return ApiClient.get<User>(`/users/${id}`);
  },

  /**
   * Lấy thông tin tài khoản cá nhân của người dùng đang đăng nhập
   */
  getMyProfile(): Promise<User> {
    return ApiClient.get<User>('/users/me');
  },

  /**
   * Cập nhật thông tin cá nhân (Họ tên, email, SĐT)
   */
  updateMyProfile(payload: UpdateProfilePayload): Promise<User> {
    return ApiClient.put<User>('/users/me', payload);
  },

  /**
   * Tạo tài khoản mới (Chỉ Quản trị viên)
   */
  createUser(payload: CreateUserPayload): Promise<User> {
    return ApiClient.post<User>('/users', payload);
  },

  /**
   * Cập nhật tài khoản và phân quyền (Chỉ Quản trị viên)
   */
  updateUser(id: number, payload: UpdateUserPayload): Promise<User> {
    return ApiClient.put<User>(`/users/${id}`, payload);
  },

  /**
   * Khóa hoặc kích hoạt lại tài khoản (Chỉ Quản trị viên)
   */
  changeStatus(id: number, payload: ChangeStatusPayload): Promise<{ message: string }> {
    return ApiClient.put<{ message: string }>(`/users/${id}/status`, payload);
  },

  /**
   * Đặt lại mật khẩu tài khoản (Chỉ Quản trị viên)
   */
  resetPassword(id: number, payload: ResetPasswordPayload): Promise<{ message: string }> {
    return ApiClient.put<{ message: string }>(`/users/${id}/reset-password`, payload);
  },

  /**
   * Xóa tài khoản (Chỉ Quản trị viên)
   */
  deleteUser(id: number): Promise<{ message: string }> {
    return ApiClient.delete<{ message: string }>(`/users/${id}`);
  },
};

export default userService;
