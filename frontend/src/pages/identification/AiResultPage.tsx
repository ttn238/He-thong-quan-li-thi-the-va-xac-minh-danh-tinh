import { useState } from 'react';
import { useParams } from 'react-router-dom';
import AiResultView from '../../components/ai-result/AiResultView';
import { AI_MOCK_ENABLED } from '../../services/aiResultService';
import type { MockScenario } from '../../services/aiResultMock';
import type { AiReview, SuggestedLocation } from '../../types/AiResult';

const SCENARIOS: { value: MockScenario; label: string }[] = [
    { value: 'processing', label: '1. Đang xử lý' },
    { value: 'success', label: '2. Hoàn thành có kết quả' },
    { value: 'empty', label: '3. Không có kết quả' },
    { value: 'error', label: '4. Lỗi dịch vụ 504' },
    { value: 'partial', label: '5. Dữ liệu khuyết thiếu' },
    { value: 'retrying', label: '6. Đang thử lại' },
];

export default function AiResultPage() {
    const { requestId = 'AI-REQ-20261014-089' } = useParams();
    const [scenario, setScenario] = useState<MockScenario>('success');

    // TẠM: chưa có API ghi vào hồ sơ / lưu biên bản nên chỉ ghi log
    const apDungDiaDiem = (l: SuggestedLocation) => { console.log('Áp dụng địa điểm', l); alert('Chưa nối API ghi vào hồ sơ (chỉ ghi log).'); };
    const luuBienBan = (r: AiReview) => { console.log('Biên bản thẩm định', r); };

    return (
        <div className= "ai-page" >
        <h2>Phân tích AI & Đối chiếu · #{ requestId } </h2>
    {
        AI_MOCK_ENABLED && (
            <div className="ai-mock-switch" >
                Dữ liệu mẫu(chỉ để kiểm tra giao diện): { ' ' }
        <select value={ scenario } onChange = {(e) => setScenario(e.target.value as MockScenario)
    }>
    { SCENARIOS.map((s) => <option key={ s.value } value = { s.value } > { s.label } </option>) }
        </select>
        </div>
      )
}
<AiResultView requestId={ requestId } scenario = { scenario } onApplyLocation = { apDungDiaDiem } onSaveReview = { luuBienBan } />
    </div>
  );
}