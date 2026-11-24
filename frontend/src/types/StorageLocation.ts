export interface StorageLocation {
  maViTri: number;
  khu: string;
  tu: string;
  ngan: string;
  sucChua: number;
  viTriDayDu?: string;
}

export interface CreateStorageLocationPayload {
  khu: string;
  tu: string;
  ngan: string;
  sucChua: number;
}

export interface UpdateStorageLocationPayload {
  khu: string;
  tu: string;
  ngan: string;
  sucChua: number;
}

export interface StorageLocationFilterParams {
  keyword?: string;
  khu?: string;
  tu?: string;
  pageIndex?: number;
  pageSize?: number;
}

export interface PagedStorageLocationResult {
  items: StorageLocation[];
  totalCount: number;
  pageIndex: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}
