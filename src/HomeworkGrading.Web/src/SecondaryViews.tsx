import { useEffect, useRef, useState } from 'react';
import { Button, Card, Dialog, InputNumber, Tag, Textarea } from 'tdesign-react';
import './secondary.css';

const paths = {
  back: 'M19 12H5m6-6-6 6 6 6',
  next: 'M5 12h14m-6-6 6 6-6 6',
  book: 'M4 19.5V5.8A1.8 1.8 0 0 1 5.8 4H11a2 2 0 0 1 2 2v14m0-14a2 2 0 0 1 2-2h5v15h-5a2 2 0 0 0-2 2 2 2 0 0 0-2-2H4Z',
  clock: 'M12 8v4l2.5 2.5M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z',
  check: 'm5 12 4 4L19 6',
  sparkles: 'M12 3l1.9 5.1L19 10l-5.1 1.9L12 17l-1.9-5.1L5 10l5.1-1.9L12 3Zm7 11v6m-3-3h6M4 3v4M2 5h4',
  writing: 'M5 4h11m-11 5h13M5 14h6m-6 5h8m2-1 4.5-4.5L21 15l-4.5 4.5L14 20l1-2Z',
  layers: 'm12 3 9 5-9 5-9-5 9-5ZM3 12l9 5 9-5M3 16l9 5 9-5',
};

function ViewIcon({ name, size = 18 }: { name: keyof typeof paths; size?: number }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.65" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <path d={paths[name]} />
    </svg>
  );
}

const mockSuggestion = {
  score: 92,
  feedback: '关系式、定义域和费用计算都正确。比较两种方案时，可以补充说明 A 方案具体便宜多少元，并在最后写出清晰的选择结论。',
};

const mathIntro = '某共享自习室提供两种收费方案：方案 A 收固定费用 10 元，另按每小时 4 元计费；方案 B 不收固定费用，每小时 6 元。';
const mathAnswer = [
  '设使用时间为 x 小时，总费用为 y 元。',
  'A 方案：y = 10 + 4x，B 方案：y = 6x。因为不能超过 8 小时，所以 0 < x ≤ 8。',
  '当 x = 6 时，A 方案是 10 + 4 × 6 = 34 元，B 方案是 6 × 6 = 36 元。我选 A 方案。',
];

export function GradingView({ onBack, assignmentTitle = '数学周练 · 一次函数' }: { onBack: () => void; assignmentTitle?: string }) {
  const [score, setScore] = useState<number | undefined>();
  const [feedback, setFeedback] = useState('');
  const [suggestionVisible, setSuggestionVisible] = useState(false);
  const [confirmApply, setConfirmApply] = useState(false);
  const [saved, setSaved] = useState<{ score: number; feedback: string }>();
  const scoreField = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const input = scoreField.current?.querySelector('input');
    if (input) input.id = 'preview-score';
  }, []);

  const changed = saved ? score !== saved.score || feedback !== saved.feedback : score !== undefined || feedback !== '';
  const validScore = score !== undefined && score >= 0 && score <= 100;

  function applySuggestion() {
    setScore(mockSuggestion.score);
    setFeedback(mockSuggestion.feedback);
    setConfirmApply(false);
  }

  return (
    <div className="preview-secondary">
      <button type="button" className="preview-back" onClick={onBack}><ViewIcon name="back" size={16} />返回作业管理</button>
      <header className="preview-heading">
        <div>
          <span className="preview-kicker">作业批改</span>
          <h1>{assignmentTitle}</h1>
          <p>对照题目和学生作答，填写分数与反馈。</p>
        </div>
        <Tag theme={saved ? 'success' : 'warning'} variant="light" shape="round">{saved ? '已批改' : '待批改'}</Tag>
      </header>

      <div className="preview-grading-grid">
        <div className="preview-panel-stack">
          <Card className="preview-view-card" bordered>
            <div className="preview-card-heading"><span className="preview-icon-tile"><ViewIcon name="book" /></span><h2>作业题目</h2></div>
            <div className="preview-question">
              <p>{mathIntro}</p>
              <ol>
                <li>分别写出总费用与使用时间的关系式，其中使用时间不超过 8 小时。</li>
                <li>如果计划使用 6 小时，选择哪种方案更划算？说明理由。</li>
              </ol>
            </div>
            <details className="preview-rubric">
              <summary>评分标准与参考答案</summary>
              <p>关系式 30 分，定义域 20 分，费用计算和方案比较 50 分。</p>
              <p>方案 A：y = 10 + 4x；方案 B：y = 6x；0 &lt; x ≤ 8。使用 6 小时时两种方案分别为 34 元、36 元，选方案 A。</p>
            </details>
          </Card>
          <Card className="preview-view-card" bordered>
            <div className="preview-card-heading preview-answer-heading">
              <span className="preview-avatar">林</span>
              <div><h2>林小满的作答</h2><span className="preview-small">11 月 14 日 09:42 提交</span></div>
            </div>
            <div className="preview-answer">
              {mathAnswer.map((paragraph) => <p key={paragraph}>{paragraph}</p>)}
            </div>
          </Card>
        </div>

        <div className="preview-panel-stack">
          <Card className="preview-view-card" bordered>
            <div className="preview-card-heading"><span className="preview-icon-tile"><ViewIcon name="writing" /></span><h2>评分与反馈</h2></div>
            <form className="preview-grading-form" onSubmit={(event) => {
              event.preventDefault();
              if (!validScore || feedback.trim() === '') return;
              setSaved({ score, feedback });
              setSuggestionVisible(false);
            }}>
              <div className="preview-field">
                <label htmlFor="preview-score">分数 <span>必填</span></label>
                <div className="preview-score-line" ref={scoreField}>
                  <InputNumber min={0} max={100} decimalPlaces={2} placeholder="输入分数" value={score ?? ''} onChange={(value) => setScore(value === '' || !Number.isFinite(Number(value)) ? undefined : Number(value))} />
                  <span>/ 100 分</span>
                </div>
              </div>
              <div className="preview-field">
                <label htmlFor="preview-feedback">给学生的反馈 <span>必填</span></label>
                <Textarea id="preview-feedback" placeholder="说说做得好的地方，以及下一步可以改进什么" value={feedback} onChange={setFeedback} maxlength={5000} autosize={{ minRows: 5, maxRows: 9 }} />
              </div>
              <div className="preview-actions">
                <Button type="submit" disabled={!validScore || feedback.trim() === '' || !changed}>保存评分</Button>
                {saved && !changed ? <span className="preview-saved" role="status"><ViewIcon name="check" size={16} />已保存：{saved.score} 分</span> : null}
              </div>
            </form>
          </Card>
          <Card className="preview-view-card preview-suggestion-card" bordered>
            <div className="preview-card-heading"><span className="preview-icon-tile preview-icon-purple"><ViewIcon name="sparkles" /></span><h2>评分建议</h2></div>
            <p className="preview-description">根据评分标准提供建议，最终分数由你确认。</p>
            {suggestionVisible ? (
              <div className="preview-suggestion-result">
                <strong>建议得分：{mockSuggestion.score} / 100</strong>
                <p>{mockSuggestion.feedback}</p>
                <Button variant="outline" onClick={() => changed ? setConfirmApply(true) : applySuggestion()}>采用建议</Button>
              </div>
            ) : (
              <Button variant="outline" icon={<ViewIcon name="sparkles" size={16} />} onClick={() => setSuggestionVisible(true)}>生成建议</Button>
            )}
          </Card>
        </div>
      </div>

      <Dialog visible={confirmApply} header="采用评分建议？" body="这会替换当前表单里尚未保存的分数和反馈。" confirmBtn="采用建议" cancelBtn="继续编辑" onClose={() => setConfirmApply(false)} onConfirm={applySuggestion} />
    </div>
  );
}

type StudentStatus = 'open' | 'pending' | 'graded';
type StudentAssignment = {
  id: string;
  title: string;
  subject: string;
  icon: keyof typeof paths;
  tone: 'blue' | 'purple' | 'green';
  due: string;
  state: StudentStatus;
  score?: number;
  description: string;
  prompt: string;
  answer?: string;
  feedback?: string;
};

const studentAssignments: StudentAssignment[] = [
  { id: 'english', title: 'My School Day · 写作练习', subject: '英语', icon: 'writing', tone: 'purple', due: '11 月 20 日 20:00', state: 'open', description: '用自己的话，写下学校里平常又特别的一天。', prompt: '请用英语写一段话，介绍你每天在学校做些什么，并写出你最喜欢的一项活动和理由。' },
  { id: 'math', title: '数学周练 · 一次函数', subject: '数学', icon: 'layers', tone: 'blue', due: '11 月 18 日 20:00', state: 'pending', description: '用一次函数解决实际问题，比较两种方案的费用。', prompt: `${mathIntro}请写出总费用与使用时间的关系式，并说明使用 6 小时时选择哪种方案。`, answer: mathAnswer.join('\n') },
  { id: 'physics', title: '光的反射 · 实验思考', subject: '物理', icon: 'book', tone: 'green', due: '11 月 12 日 20:00', state: 'graded', description: '回顾课堂实验，解释镜面反射的规律。', prompt: '在平面镜反射实验中，如果入射光线与镜面的夹角是 30°，反射角是多少？请说明依据。', answer: '入射角以法线为基准，所以入射角是 90° − 30° = 60°。反射角等于入射角，也是 60°。', score: 96, feedback: '结论和推导都正确。若在示意图上标出法线和两个角，过程会更清楚。' },
];

const statusLabel = { open: '待提交', pending: '待批改', graded: '已批改' } as const;
const statusTheme = { open: 'primary', pending: 'warning', graded: 'success' } as const;
const studentFilters = ['all', 'open', 'pending', 'graded'] as const;

export function StudentView() {
  const [assignments, setAssignments] = useState(studentAssignments);
  const [filter, setFilter] = useState<(typeof studentFilters)[number]>('all');
  const [selected, setSelected] = useState<string>();
  const [draft, setDraft] = useState('');
  const [confirmSubmit, setConfirmSubmit] = useState(false);
  const active = assignments.find((assignment) => assignment.id === selected);
  const visibleAssignments = assignments.filter((assignment) => filter === 'all' || assignment.state === filter);

  if (active) return (
    <div className="preview-secondary">
      <button type="button" className="preview-back" onClick={() => setSelected(undefined)}><ViewIcon name="back" size={16} />返回我的作业</button>
      <header className="preview-heading">
        <div><span className="preview-kicker">{active.subject}</span><h1>{active.title}</h1><p>截止时间：{active.due}　·　满分 100</p></div>
        <Tag theme={statusTheme[active.state]} variant="light" shape="round">{statusLabel[active.state]}</Tag>
      </header>
      <div className="preview-panel-stack preview-student-detail">
        <Card className="preview-view-card" bordered>
          <div className="preview-card-heading"><span className={`preview-icon-tile preview-icon-${active.tone}`}><ViewIcon name={active.icon} /></span><h2>作业题目</h2></div>
          <p className="preview-description">{active.prompt}</p>
        </Card>
        {active.state === 'graded' ? (
          <Card className="preview-view-card preview-grade-result" bordered>
            <div className="preview-card-heading"><span className="preview-icon-tile preview-icon-green"><ViewIcon name="check" /></span><h2>批改结果</h2></div>
            <p className="preview-result-score"><strong>{active.score}</strong><span> / 100 分</span></p>
            <p className="preview-description">{active.feedback}</p>
          </Card>
        ) : null}
        <Card className="preview-view-card" bordered>
          <div className="preview-card-heading"><span className="preview-icon-tile"><ViewIcon name="writing" /></span><h2 id="preview-student-answer-title">我的作答</h2></div>
          {active.state === 'open' ? (
            <div className="preview-grading-form">
              <Textarea aria-labelledby="preview-student-answer-title" placeholder="在这里写下你的答案…" value={draft} onChange={setDraft} maxlength={30000} autosize={{ minRows: 6, maxRows: 12 }} />
              <div className="preview-actions"><Button disabled={draft.trim() === ''} onClick={() => setConfirmSubmit(true)}>提交作业</Button><span className="preview-small">每份作业只能提交一次</span></div>
            </div>
          ) : (
            <><div className="preview-answer">{active.answer}</div>{active.state === 'pending' ? <p className="preview-small preview-after-answer">已提交，等老师批改后可在此查看成绩。</p> : null}</>
          )}
        </Card>
      </div>
      <Dialog visible={confirmSubmit} header="确认提交作业？" body="提交后无法再修改答案。" confirmBtn="确认提交" cancelBtn="继续作答" onClose={() => setConfirmSubmit(false)} onConfirm={() => {
        setAssignments((items) => items.map((item) => item.id === active.id ? { ...item, state: 'pending', answer: draft.trim() } : item));
        setConfirmSubmit(false);
        setDraft('');
      }} />
    </div>
  );

  return (
    <div className="preview-secondary">
      <header className="preview-student-banner">
        <div><span className="preview-kicker">学生空间</span><h1>我的作业</h1><p>查看老师发布的作业，按时提交，及时查看反馈。</p></div>
        <div className="preview-student-decoration" aria-hidden="true"><span /><span /><span><ViewIcon name="book" size={34} /></span></div>
      </header>
      <div className="preview-student-list-head">
        <h2>作业列表</h2>
        <div className="preview-filter-group" aria-label="作业状态筛选">
          {studentFilters.map((value) => (
            <Button key={value} size="small" theme={filter === value ? 'primary' : 'default'} variant="text" aria-pressed={filter === value} onClick={() => setFilter(value)}>
              {value === 'all' ? '全部' : statusLabel[value]}
              <span className="preview-filter-count">{value === 'all' ? assignments.length : assignments.filter((assignment) => assignment.state === value).length}</span>
            </Button>
          ))}
        </div>
      </div>
      {visibleAssignments.length ? (
        <div className="preview-student-grid">
          {visibleAssignments.map((assignment) => (
            <Card className="preview-view-card preview-assignment-card" key={assignment.id} bordered>
              <div className="preview-assignment-meta">
                <span className={`preview-icon-tile preview-icon-${assignment.tone}`}><ViewIcon name={assignment.icon} /></span>
                <Tag theme={statusTheme[assignment.state]} variant="light" shape="round">{statusLabel[assignment.state]}</Tag>
              </div>
              <p className="preview-subject">{assignment.subject}</p>
              <h3>{assignment.title}</h3>
              <p className="preview-description">{assignment.description}</p>
              <div className="preview-assignment-footer">
                <span className="preview-small"><ViewIcon name="clock" size={14} />{assignment.due}</span>
                <Button theme="primary" variant="text" size="small" suffix={<ViewIcon name="next" size={14} />} onClick={() => { setSelected(assignment.id); setDraft(''); }}>
                  {assignment.state === 'open' ? '去作答' : assignment.state === 'graded' ? '查看成绩' : '查看作业'}
                </Button>
              </div>
            </Card>
          ))}
        </div>
      ) : (
        <Card className="preview-view-card preview-empty" bordered><span className="preview-icon-tile"><ViewIcon name="book" /></span><h3>暂无此状态的作业</h3><p className="preview-small">可以切换到「全部」查看其他作业。</p></Card>
      )}
    </div>
  );
}
