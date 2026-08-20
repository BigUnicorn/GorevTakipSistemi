/**
 * Bu dosya, backend (GorevTakip.Entities.DTOs) modellerine birebir karşılık gelecek şekilde
 * oluşturulmuş TypeScript tiplerini barındırır.
 * (Type Safety / Tip Güvenliği)
 */

export interface components {
  schemas: {
    /**
     * GorevTakip.Entities.DTOs.TaskResponseDto
     */
    TaskDto: {
      id: number;
      title: string;
      description: string;
      status: number; // 1: Todo, 2: InProgress, 3: Done
      createdDate: string;
      dueDate?: string | null;
      assignedUserId: number;
      assignedUserName: string;
      category: number; // 1: Frontend, 2: Backend, vb.
      isOverdue: boolean;
    };

    /**
     * GorevTakip.Entities.DTOs.TaskCommentDto
     */
    TaskCommentDto: {
      id: number;
      text: string;
      userName: string;
      createdDate: string;
    };

    /**
     * GorevTakip.Entities.DTOs.TaskHistoryDto
     */
    TaskHistoryDto: {
      actionMessage: string;
      createdDate: string;
    };

    /**
     * GorevTakip.Entities.TaskAttachment tabanlı dönüş
     */
    TaskAttachmentDto: {
      id: number;
      taskId: number;
      fileName: string;
      filePath: string;
      contentType: string;
      fileSize: number;
      uploadedAt: string;
      uploadedByUserName: string;
    };
  };
}
