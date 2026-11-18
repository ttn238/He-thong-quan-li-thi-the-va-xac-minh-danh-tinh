import ApiClient from './api';
import type { AiResult, ReviewInput, ReviewRecord } from '../types/AiResult';

const BASE = '/identification/images';

function normalize(raw: Partial<AiResult>): AiResult {
    return {
        ...raw,
        requestId: raw.requestId ?? '',
        status: raw.status ?? 'processing',
        ocr: raw.ocr ?? [],
        objects: raw.objects ?? [],
        locations: raw.locations ?? [],
        missingFields: raw.missingFields ?? [],
        image: raw.image ? { ...raw.image, url: undefined } : undefined,
    } as AiResult;
}

export const aiResultService = {
    async analyze(file: File, linkedRecordId?: string): Promise<AiResult> {
        const form = new FormData();
        form.append('file', file);
        if (linkedRecordId?.trim()) form.append('linkedRecordId', linkedRecordId.trim());
        return normalize(await ApiClient.post<Partial<AiResult>>(BASE, form));
    },

    async getAiResult(requestId: string): Promise<AiResult> {
        const raw = await ApiClient.get<Partial<AiResult>>(`${BASE}/${encodeURIComponent(requestId)}`);
        return normalize(raw);
    },

    getImage(requestId: string): Promise<Blob> {
        return ApiClient.getBlob(`${BASE}/${encodeURIComponent(requestId)}/image`);
    },

    saveReview(requestId: string, input: ReviewInput): Promise<ReviewRecord> {
        return ApiClient.post<ReviewRecord>(`${BASE}/${encodeURIComponent(requestId)}/review`, input);
    },
};
