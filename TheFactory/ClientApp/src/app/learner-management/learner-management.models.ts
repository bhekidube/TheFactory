export interface LearnerDto {
  id: number;
  firstName: string;
  surname: string;
  grade: string;
}

export interface SubjectDto {
  id: number;
  name: string;
  code: string;
  isActive: boolean;
}

export interface SubjectScoreDto {
  learnerId?: number;
  subjectId: number;
  subject: string;
  possibleMark: number;
  pupilMark: number;
  grade: string;
  teacherComments: string;
  score?: number;
}

export interface SubjectScoreUpsertRequest {
  learnerId: number;
  subjectId: number;
  possibleMark: number;
  pupilMark: number;
  grade: string;
  teacherComments: string;
  score?: number;
}

export interface LearnerReportResponseDto {
  schoolName: string;
  learnerName: string;
  grade: string;
  subjects: SubjectScoreDto[];
  average: number;
}

export interface TeacherLookupDto {
  teacherId: number;
  name: string;
  email: string;
  role: string;
}

export interface LearnerLookupDto {
  learnerId: number;
  firstName: string;
  surname: string;
  grade: string;
}

export interface ClassDto {
  id: number;
  schoolId: number;
  name: string;
  grade: string;
  teacherId: number;
  teacherName: string;
  learnerIds: number[];
}

export interface CreateClassRequestDto {
  name: string;
  grade: string;
  teacherId: number;
  learnerIds: number[];
}
