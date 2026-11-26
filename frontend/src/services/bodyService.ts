import ApiClient from './api';
import type {
  BodyCharacteristic,
  BodyFilterParams,
  BodyListResponse,
  BodyRecord,
  SaveBodyCharacteristicPayload,
  SaveBodyCharacteristicResponse,
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

  async getCharacteristics(bodyId: number): Promise<BodyCharacteristic[]> {
    return ApiClient.get<BodyCharacteristic[]>(`/bodies/${bodyId}/characteristics`);
  },

  async createCharacteristic(
    bodyId: number,
    payload: SaveBodyCharacteristicPayload,
  ): Promise<SaveBodyCharacteristicResponse> {
    return ApiClient.post<SaveBodyCharacteristicResponse>(`/bodies/${bodyId}/characteristics`, payload);
  },

  async updateCharacteristic(
    bodyId: number,
    characteristicId: number,
    payload: SaveBodyCharacteristicPayload,
  ): Promise<SaveBodyCharacteristicResponse> {
    return ApiClient.put<SaveBodyCharacteristicResponse>(
      `/bodies/${bodyId}/characteristics/${characteristicId}`,
      payload,
    );
  },
};

export default bodyService;
