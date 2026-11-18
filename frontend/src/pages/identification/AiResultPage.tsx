import { useNavigate, useParams } from 'react-router-dom';
import AiResultView from '../../components/ai-result/AiResultView';
import AiUploadBox from '../../components/ai-result/AiUploadBox';
import { S } from '../../components/ai-result/aiResultStyles';
import { authService } from '../../services/authService';

export default function AiResultPage() {
    const { requestId } = useParams();
    const navigate = useNavigate();
    const roleId = authService.getSession()?.user.maVaiTro;
    const canAccessAi = roleId === 1 || roleId === 2 || roleId === 3;
    return <main className="mx-auto max-w-300 px-4 py-4 md:px-6">
        <div className="mb-3 flex items-center justify-between gap-2">
            <h2 className="m-0 text-xl font-bold text-navy">Phân tích ảnh & kết quả AI{requestId ? ` · #${requestId}` : ''}</h2>
            {requestId && <button className={S.btnGhost} onClick={() => navigate('/identification/ai-result')}>Phân tích ảnh khác</button>}
        </div>
        {!canAccessAi
            ? <p className="rounded border border-amber-300 bg-amber-50 p-4 text-sm text-amber-900" role="alert">
                Tài khoản hiện tại chưa được cấp quyền phân tích ảnh hoặc xem kết quả AI.
            </p>
            : requestId
            ? <AiResultView key={requestId} requestId={requestId} />
            : <AiUploadBox onUploaded={(id) => navigate(`/identification/ai-result/${id}`)} />}
    </main>;
}
