export type AssignmentStatus = 'open' | 'pending' | 'graded' | 'closed';

export type DemoAssignment = {
  id: string;
  title: string;
  subject: string;
  dueAt: string;
  submitted: number;
  graded: number;
  status: AssignmentStatus;
};

export const assignments: DemoAssignment[] = [
  { id: 'math', title: '数学周练 · 一次函数', subject: '数学', dueAt: '2026-11-18T20:00', submitted: 28, graded: 18, status: 'pending' },
  { id: 'chinese', title: '《背影》阅读与赏析', subject: '语文', dueAt: '2026-11-17T21:00', submitted: 26, graded: 22, status: 'pending' },
  { id: 'english', title: 'My School Day · 写作练习', subject: '英语', dueAt: '2026-11-20T20:00', submitted: 0, graded: 0, status: 'open' },
  { id: 'physics', title: '光的反射 · 实验思考', subject: '物理', dueAt: '2026-11-12T20:00', submitted: 31, graded: 31, status: 'graded' },
  { id: 'history', title: '丝绸之路 · 课后思考', subject: '历史', dueAt: '2026-11-10T19:30', submitted: 24, graded: 24, status: 'graded' },
  { id: 'writing', title: '写给未来的自己', subject: '语文', dueAt: '2026-11-21T20:00', submitted: 0, graded: 0, status: 'open' },
  { id: 'geography', title: '读图练习 · 中国地形', subject: '地理', dueAt: '2026-11-23T20:00', submitted: 0, graded: 0, status: 'open' },
  { id: 'revision', title: '期中复习 · 错题整理', subject: '数学', dueAt: '2026-11-05T20:00', submitted: 0, graded: 0, status: 'closed' },
];

export const statusLabels: Record<AssignmentStatus, string> = {
  open: '待提交',
  pending: '待批改',
  graded: '已批改',
  closed: '已截止',
};

export function formatDate(date: string) {
  return date.replace('T', ' ').replace(/^2026-/, '').replace('-', '/');
}
