export interface LearnerDto {
  id: number;
  firstName: string;
  surname: string;
  grade: string;
}

export interface SubjectScoreDto {
  subject: string;
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
