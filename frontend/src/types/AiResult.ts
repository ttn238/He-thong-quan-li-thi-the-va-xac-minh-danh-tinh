// Hợp đồng JSON do dịch vụ AI (ai-service) trả về. Tên trường là camelCase.
export type BBox = [number, number, number, number]; // [x, y, rộng, cao] theo pixel ảnh gốc

export interface OcrItem {
    text: string;
    kind?: 'street_sign' | 'license_plate' | 'other';
    confidence: number; // 0..1
    bbox?: BBox;
    note?: string;
}

export interface DetectedObject {
    label: string;
    description?: string;
    confidence: number; // 0..1
    bbox?: BBox;
    tag?: string;
}

export interface SuggestedLocation {
    rank: number;
    name: string;
    confidence: number; // 0..1
    adminArea?: string;
    lat?: number;
    lng?: number;
    basis?: string;
}

export interface MissingField {
    field: string;
    status: 'done' | 'missing' | 'out_of_scope' | 'unsupported';
    value?: string;
    recommendation?: string;
}

export interface AiResultError {
    code: string;
    message: string;
    httpStatus?: number;
    traceId?: string;
    occurredAt?: string;
}

export interface AiResultImage {
    fileName: string;
    sizeBytes?: number;
    format?: string;
    width?: number;
    height?: number;
    url?: string;
}

export type ReviewDecision = 'confirm' | 'need_more' | 'reject';

export interface ReviewInput {
    decision: ReviewDecision;
    checkedItems: string[];
    note: string;
}

export interface ReviewRecord extends ReviewInput {
    reviewer?: string;
    reviewedAt: string;
}

export type AiJobStatus = 'processing' | 'retrying' | 'completed' | 'failed';

export interface AiResult {
    requestId: string;
    status: AiJobStatus;
    progress?: number;
    attempt?: number;
    linkedRecordId?: string;
    submittedAt?: string;
    completedAt?: string;
    durationMs?: number;
    engine?: string;
    imageHash?: string;
    image?: AiResultImage;
    ocr: OcrItem[];
    objects: DetectedObject[];
    locations: SuggestedLocation[];
    missingFields: MissingField[];
    error?: AiResultError;
    review?: ReviewRecord;
}

// Trạng thái hiển thị của giao diện (suy ra từ AiResult + trạng thái tải)
export type AiViewState =
    | 'loading' | 'processing' | 'retrying' | 'success' | 'partial' | 'empty' | 'error';