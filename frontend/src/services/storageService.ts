import { ApiClient } from './api';
import {
  StorageLocation,
  CreateStorageLocationPayload,
  UpdateStorageLocationPayload,
  StorageLocationFilterParams,
  PagedStorageLocationResult,
  StorageMovementItem,
} from '../types/StorageLocation';

export const storageService = {
  /**
   * Lấy danh sách vị trí lưu trữ có phân trang và lọc theo Khu, Tủ, từ khóa
   */
  async getLocations(params?: StorageLocationFilterParams): Promise<PagedStorageLocationResult> {
    const query = new URLSearchParams();
    if (params?.keyword) query.set('keyword', params.keyword);
    if (params?.khu) query.set('khu', params.khu);
    if (params?.tu) query.set('tu', params.tu);
    if (params?.pageIndex) query.set('pageIndex', params.pageIndex.toString());
    if (params?.pageSize) query.set('pageSize', params.pageSize.toString());

    const qs = query.toString();
    const endpoint = `/storage-locations${qs ? `?${qs}` : ''}`;
    return await ApiClient.request<PagedStorageLocationResult>(endpoint);
  },

  /**
   * Lấy chi tiết thông tin một vị trí lưu trữ
   */
  async getLocationById(id: number): Promise<StorageLocation> {
    return await ApiClient.request<StorageLocation>(`/storage-locations/${id}`);
  },

  /**
   * Lấy danh sách tên các Khu lưu trữ hiện có để nạp vào dropdown bộ lọc
   */
  async getAreas(): Promise<string[]> {
    return await ApiClient.request<string[]>('/storage-locations/areas');
  },

  /**
   * Thêm mới một vị trí lưu trữ (Khu, Tủ, Ngăn, Sức chứa)
   */
  async createLocation(payload: CreateStorageLocationPayload): Promise<StorageLocation> {
    return await ApiClient.request<StorageLocation>('/storage-locations', {
      method: 'POST',
      body: JSON.stringify(payload),
    });
  },

  /**
   * Cập nhật thông tin vị trí lưu trữ
   */
  async updateLocation(id: number, payload: UpdateStorageLocationPayload): Promise<StorageLocation> {
    return await ApiClient.request<StorageLocation>(`/storage-locations/${id}`, {
      method: 'PUT',
      body: JSON.stringify(payload),
    });
  },

  /**
   * Xóa một vị trí lưu trữ (chỉ xóa được nếu chưa có lịch sử lưu trữ thi thể)
   */
  async deleteLocation(id: number): Promise<{ message: string }> {
    return await ApiClient.request<{ message: string }>(`/storage-locations/${id}`, {
      method: 'DELETE',
    });
  },

  /**
   * Tra cứu dòng thời gian lịch sử xếp và luân chuyển vị trí của hồ sơ thi thể (Task 7.7)
   * GET /api/v1/storage/history/{maHoSoThiThe}
   */
  async getMovementHistory(maHoSoThiThe: number): Promise<StorageMovementItem[]> {
    return await ApiClient.get<StorageMovementItem[]>(`/storage/history/${maHoSoThiThe}`);
  },
};

export default storageService;
