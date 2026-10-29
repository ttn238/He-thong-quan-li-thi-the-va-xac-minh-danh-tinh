import ApiClient, { AI_API_URL } from './api';
import type { AiResult, ReviewInput, ReviewRecord } from '../types/AiResult';

const BASE = '/v1/results';

// JSON từ máy chủ có thể chứa null / thiếu mảng: chuẩn hóa để giao diện không bao giờ gặp null/undefined bất ngờ
function chuanHoa(raw: Partial<AiResult>): AiResult {
    const sach = JSON.parse(JSON.stringify(raw, (_k, v) => (v === null ? undefined : v))) as Partial<AiResult>;
    return {
        ...sach,
        requestId: sach.requestId ?? '',
        status: sach.status ?? 'processing',
        ocr: sach.ocr ?? [],
        objects: sach.objects ?? [],
        locations: sach.locations ?? [],
        missingFields: sach.missingFields ?? [],
        // Máy chủ trả đường dẫn tương đối; ghép với địa chỉ gốc của dịch vụ AI
        image: sach.image
            ? { ...sach.image, url: sach.image.url ? `${AI_API_URL}${sach.image.url}` : undefined }
            : undefined,
    };
}

export const aiResultService = {
    /** Tải ảnh lên và chạy phân tích (đồng bộ). Trả về kết quả kèm mã yêu cầu. */
    async analyze(file: File, linkedRecordId?: string): Promise<AiResult> {
        const form = new FormData();
        form.append('file', file);
        if (linkedRecordId?.trim()) form.append('linked_record_id', linkedRecordId.trim());
        const raw = await ApiClient.post<Partial<AiResult>>(BASE, form, AI_API_URL);
        return chuanHoa(raw);
    },

    async getAiResult(requestId: string): Promise<AiResult> {
        const raw = await ApiClient.get<Partial<AiResult>>(
            `${BASE}/${encodeURIComponent(requestId)}`, undefined, AI_API_URL);
        return chuanHoa(raw);
    },

    /** Lưu biên bản thẩm định của cán bộ. `reviewer` chỉ là nhãn hiển thị (chưa được xác thực ở dịch vụ AI). */
    saveReview(requestId: string, input: ReviewInput, reviewer?: string): Promise<ReviewRecord> {
        return ApiClient.post<ReviewRecord>(
            `${BASE}/${encodeURIComponent(requestId)}/review`, { ...input, reviewer }, AI_API_URL);
    },
};