import ApiClient from './api';
import type { MissingPersonDetail } from '../types/MissingPerson';

export const missingPersonService = {
  /**
   * Lấy chi tiết đầy đủ hồ sơ người mất tích (GET /api/v1/missing-persons/{id}).
   */
  async getById(id: number): Promise<MissingPersonDetail> {
    return ApiClient.get<MissingPersonDetail>(`/missing-persons/${id}`);
  },
};

export default missingPersonService;
