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

export interface ClassAssignedSubjectDto {
  subjectId: number;
  name: string;
  code: string;
}

export interface ClassDetailDto {
  id: number;
  schoolId: number;
  name: string;
  grade: string;
  teacherId: number;
  teacherName: string;
  learners: LearnerLookupDto[];
  subjects: ClassAssignedSubjectDto[];
}

export interface SubjectUpsertRequestDto {
  name: string;
  code: string;
  isActive: boolean;
}

export interface LearnerAcademicRecordDto {
  subjectName: string;
  subjectCode: string;
  termOrPeriod: string;
  gradeOrMarkPercent: number;
  teacherRemarks: string;
}

export interface LearnerDetailDto {
  learnerId: number;
  firstName: string;
  surname: string;
  grade: string;
  dateOfBirth: string;
  parentGuardianContact: string;
  academicRecords: LearnerAcademicRecordDto[];
}

export interface WorkDto {
  id: number;
  schoolId: number;
  title: string;
  subjectId: number;
  subjectName: string;
  description: string;
  workTypeId: number;
  workType: string;
  classId: number;
  className: string;
  dueDate: string;
  totalMark: number;
  isArchived: boolean;
}

export interface WorkUpsertRequestDto {
  title: string;
  subjectId: number;
  description: string;
  workTypeId: number;
  classId: number;
  dueDate: string;
  totalMark: number;
}

export interface WorkTypeLookupDto {
  id: number;
  name: string;
}

export interface WorkLearnerMarkDto {
  learnerId: number;
  learnerName: string;
  grade: string;
  totalMark: number;
  markObtained: number | null;
  comment: string;
}

export interface WorkDetailDto {
  workId: number;
  classId: number;
  title: string;
  subjectName: string;
  workType: string;
  dueDate: string;
  totalMark: number;
  learners: WorkLearnerMarkDto[];
}

export interface WorkLearnerMarkUpsertDto {
  learnerId: number;
  markObtained: number | null;
  comment: string;
}

export interface SaveWorkMarksRequestDto {
  marks: WorkLearnerMarkUpsertDto[];
}
