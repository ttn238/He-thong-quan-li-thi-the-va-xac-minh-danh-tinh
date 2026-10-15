import ApiClient from './api';
import {
  Role,
  Permission,
  ResourceGroupedPermissions,
  AssignRolePayload,
  UpdateRolePermissionsPayload,
} from '../types/Role';

/**
 * Dịch vụ gọi API Quản lý Vai trò và Phân quyền (Role & Permission)
 */
export const roleService = {
  /**
   * Lấy danh sách 5 vai trò hệ thống kèm số lượng tài khoản (Chỉ Quản trị viên)
   */
  getRoles(): Promise<Role[]> {
    return ApiClient.get<Role[]>('/roles');
  },

  /**
   * Xem thông tin chi tiết một vai trò và danh sách quyền hạn
   */
  getRoleById(id: number): Promise<Role> {
    return ApiClient.get<Role>(`/roles/${id}`);
  },

  /**
   * Lấy toàn bộ danh mục quyền hệ thống theo nhóm Resource
   */
  getPermissions(): Promise<ResourceGroupedPermissions[]> {
    return ApiClient.get<ResourceGroupedPermissions[]>('/roles/permissions');
  },

  /**
   * Gán hoặc thay đổi vai trò cho một tài khoản người dùng
   */
  assignRole(payload: AssignRolePayload): Promise<{ message: string }> {
    return ApiClient.put<{ message: string }>('/roles/assign', payload);
  },

  /**
   * Cập nhật ma trận phân quyền cho vai trò
   */
  updateRolePermissions(id: number, payload: UpdateRolePermissionsPayload): Promise<{ message: string }> {
    return ApiClient.put<{ message: string }>(`/roles/${id}/permissions`, payload);
  },
};

export default roleService;
