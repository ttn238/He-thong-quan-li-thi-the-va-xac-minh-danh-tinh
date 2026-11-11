/**
 * Định nghĩa kiểu dữ liệu Vai trò & Phân quyền hệ thống (RBAC)
 */

export interface Permission {
  maQuyen: number;
  resource: string;
  action: string;
  tenQuyen: string;
  moTa?: string;
  phamVi?: 'ALL' | 'OWN' | 'PARTIAL';
}

export interface ResourceGroupedPermissions {
  resource: string;
  permissions: Permission[];
}

export interface Role {
  maVaiTro: number;
  tenVaiTro: string;
  maCode: string;
  moTa?: string;
  userCount: number;
  permissions?: Permission[];
}

export interface AssignRolePayload {
  maTaiKhoan: number;
  maVaiTro: number;
  lyDo?: string;
}

export interface UpdateRolePermissionsPayload {
  permissions: {
    maQuyen: number;
    phamVi: 'ALL' | 'OWN' | 'PARTIAL';
  }[];
}
