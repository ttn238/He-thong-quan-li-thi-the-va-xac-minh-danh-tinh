import ApiClient from './api';
import type {
  BodyFilterParams,
  BodyListResponse,
  BodyRecord,
  UpdateBodyPayload,
  UpdateBodyResponse,
} from '../types/Body';

export const bodyService = {
  async getBodies(params?: BodyFilterParams): Promise<BodyListResponse> {
    return ApiClient.get<BodyListResponse>('/bodies', params);
  },

  async getBodyById(id: number): Promise<BodyRecord> {
    return ApiClient.get<BodyRecord>(`/bodies/${id}`);
  },

  async updateBody(id: number, payload: UpdateBodyPayload): Promise<UpdateBodyResponse> {
    return ApiClient.put<UpdateBodyResponse>(`/bodies/${id}`, payload);
  },
};

export default bodyService;
