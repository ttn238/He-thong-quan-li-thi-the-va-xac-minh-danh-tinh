import ApiClient from './api';
import type {
  BodyCharacteristic,
  BodyDetail,
  BodyFilterParams,
  BodyImageItem,
  BodyImageUploadResult,
  BodyListResponse,
  BodyRecord,
  LoaiAnhHoSoThiThe,
  SaveBodyCharacteristicPayload,
  SaveBodyCharacteristicResponse,
  UpdateBodyPayload,
  UpdateBodyResponse,
} from '../types/Body';

const BASE = '/bodies';

/** Định dạng dung lượng ảnh cho UI (backend chỉ sinh sẵn nhãn này trong chi tiết hồ sơ). */
export function dingDangKichThuocAnh(soByte: number): string {
  if (soByte >= 1024 * 1024) return `${(soByte / (1024 * 1024)).toFixed(2)} MB`;
  return `${(soByte / 1024).toFixed(1)} KB`;
}

function toImageItem(anh: BodyImageUploadResult): BodyImageItem {
  return {
    maHinhAnh: anh.maHinhAnh,
    tenGoc: anh.tenGoc,
    duongDan: anh.duongDan,
    loaiHinhAnh: anh.loaiHinhAnh,
    dinhDang: anh.dinhDang,
    kichThuoc: anh.kichThuoc,
    laNhayCam: anh.laNhayCam,
    moTa: anh.moTa,
    thoiGianTaiLen: anh.thoiGianTaiLen,
    nguoiTaiLen: anh.nguoiTaiLen,
    kichThuocHienThi: dingDangKichThuocAnh(anh.kichThuoc),
    nhanNhayCam: anh.laNhayCam ? 'Nhạy cảm' : 'Công khai',
  };
}

export const bodyService = {
  async getBodies(params?: BodyFilterParams): Promise<BodyListResponse> {
    return ApiClient.get<BodyListResponse>(BASE, params);
  },

  async getBodyById(id: number): Promise<BodyRecord> {
    return ApiClient.get<BodyRecord>(`${BASE}/${id}`);
  },

  getDetail(maHoSoThiThe: number): Promise<BodyDetail> {
    return ApiClient.get<BodyDetail>(`${BASE}/${maHoSoThiThe}`);
  },

  async updateBody(id: number, payload: UpdateBodyPayload): Promise<UpdateBodyResponse> {
    return ApiClient.put<UpdateBodyResponse>(`${BASE}/${id}`, payload);
  },

  async getCharacteristics(bodyId: number): Promise<BodyCharacteristic[]> {
    return ApiClient.get<BodyCharacteristic[]>(`${BASE}/${bodyId}/characteristics`);
  },

  async createCharacteristic(
    bodyId: number,
    payload: SaveBodyCharacteristicPayload,
  ): Promise<SaveBodyCharacteristicResponse> {
    return ApiClient.post<SaveBodyCharacteristicResponse>(`${BASE}/${bodyId}/characteristics`, payload);
  },

  async updateCharacteristic(
    bodyId: number,
    characteristicId: number,
    payload: SaveBodyCharacteristicPayload,
  ): Promise<SaveBodyCharacteristicResponse> {
    return ApiClient.put<SaveBodyCharacteristicResponse>(
      `${BASE}/${bodyId}/characteristics/${characteristicId}`,
      payload,
    );
  },

  /**
   * Tải ảnh lên một hồ sơ thi thể. Mã hồ sơ luôn nằm trên đường dẫn;
   * backend tự gắn cờ nhạy cảm và chọn vùng lưu trữ, máy khách không gửi được hai giá trị này.
   */
  async uploadImage(
    maHoSoThiThe: number,
    file: File,
    loaiHinhAnh?: LoaiAnhHoSoThiThe,
    moTa?: string,
  ): Promise<BodyImageItem> {
    const form = new FormData();
    form.append('file', file);
    if (loaiHinhAnh) form.append('loaiHinhAnh', loaiHinhAnh);
    if (moTa?.trim()) form.append('moTa', moTa.trim());

    const created = await ApiClient.post<BodyImageUploadResult>(`${BASE}/${maHoSoThiThe}/images`, form);
    return toImageItem(created);
  },

  /** Ảnh hồ sơ thi thể không có URL công khai: chỉ lấy được byte qua endpoint có JWT. */
  getImage(maHoSoThiThe: number, maHinhAnh: number): Promise<Blob> {
    return ApiClient.getBlob(`${BASE}/${maHoSoThiThe}/images/${maHinhAnh}`);
  },
};

export default bodyService;
