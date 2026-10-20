import {
  AfterViewInit,
  Component,
  ElementRef,
  EventEmitter,
  HostListener,
  Inject,
  Output,
  PLATFORM_ID,
  ViewChild
} from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

interface SignaturePoint {
  x: number;
  y: number;
}

@Component({
  selector: 'app-documents',
  templateUrl: './documents.component.html',
  styleUrls: ['./documents.component.css']
})
export class DocumentsComponent implements AfterViewInit {
  @ViewChild('signatureCanvas') private signatureCanvas!: ElementRef<HTMLCanvasElement>;
  @Output() readonly uploadReady = new EventEmitter<FormData>();

  title = '';
  selectedFile: File | null = null;
  statusMessage = '';
  statusType: 'error' | 'success' = 'success';

  private readonly strokes: SignaturePoint[][] = [];
  private currentStroke: SignaturePoint[] | null = null;
  private context: CanvasRenderingContext2D | null = null;

  constructor(@Inject(PLATFORM_ID) private readonly platformId: object) {}

  get signatureDrawn(): boolean {
    return this.strokes.length > 0;
  }

  ngAfterViewInit(): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    this.context = this.signatureCanvas.nativeElement.getContext('2d');
    if (!this.context) {
      this.setStatus('Signature pad is unavailable in this browser.', 'error');
      return;
    }

    this.resizeCanvas();
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    this.statusMessage = '';

    if (!file) {
      this.selectedFile = null;
      return;
    }

    if (!this.isSupportedFile(file)) {
      this.selectedFile = null;
      input.value = '';
      this.setStatus('Choose a PDF, Word document, or image file.', 'error');
      return;
    }

    this.selectedFile = file;
  }

  onPointerDown(event: PointerEvent): void {
    if (!this.context) {
      return;
    }

    event.preventDefault();
    const point = this.getPoint(event);
    this.currentStroke = [point];
    this.strokes.push(this.currentStroke);
    this.signatureCanvas.nativeElement.setPointerCapture(event.pointerId);
    this.redrawSignature();
  }

  onPointerMove(event: PointerEvent): void {
    if (!this.currentStroke) {
      return;
    }

    event.preventDefault();
    this.currentStroke.push(this.getPoint(event));
    this.redrawSignature();
  }

  onPointerUp(event: PointerEvent): void {
    if (this.currentStroke) {
      event.preventDefault();
      this.currentStroke = null;
    }
  }

  clearSignature(): void {
    this.strokes.length = 0;
    this.currentStroke = null;
    this.redrawSignature();
    this.statusMessage = '';
  }

  async saveAndUpload(): Promise<void> {
    if (!this.title.trim()) {
      this.setStatus('Enter a document title.', 'error');
      return;
    }

    if (!this.selectedFile) {
      this.setStatus('Choose a document to upload.', 'error');
      return;
    }

    if (this.strokes.length === 0) {
      this.setStatus('Add a signature before saving.', 'error');
      return;
    }

    const canvas = this.signatureCanvas.nativeElement;
    const signature = await new Promise<Blob | null>(resolve => canvas.toBlob(resolve, 'image/png'));
    if (!signature) {
      this.setStatus('Unable to prepare the signature image.', 'error');
      return;
    }

    const payload = new FormData();
    payload.append('title', this.title.trim());
    payload.append('file', this.selectedFile, this.selectedFile.name);
    payload.append('signature', signature, 'signature.png');
    this.uploadReady.emit(payload);
    this.setStatus('Document package is ready for backend upload.', 'success');
  }

  @HostListener('window:resize')
  onWindowResize(): void {
    if (this.context) {
      this.resizeCanvas();
    }
  }

  private resizeCanvas(): void {
    const canvas = this.signatureCanvas.nativeElement;
    const bounds = canvas.getBoundingClientRect();
    if (bounds.width === 0 || bounds.height === 0 || !this.context) {
      return;
    }

    const pixelRatio = isPlatformBrowser(this.platformId) ? window.devicePixelRatio || 1 : 1;
    canvas.width = Math.round(bounds.width * pixelRatio);
    canvas.height = Math.round(bounds.height * pixelRatio);
    this.context.setTransform(pixelRatio, 0, 0, pixelRatio, 0, 0);
    this.redrawSignature();
  }

  private getPoint(event: PointerEvent): SignaturePoint {
    const canvas = this.signatureCanvas.nativeElement;
    const bounds = canvas.getBoundingClientRect();
    return {
      x: (event.clientX - bounds.left) / bounds.width,
      y: (event.clientY - bounds.top) / bounds.height
    };
  }

  private redrawSignature(): void {
    if (!this.context) {
      return;
    }

    const canvas = this.signatureCanvas.nativeElement;
    const bounds = canvas.getBoundingClientRect();
    this.context.clearRect(0, 0, bounds.width, bounds.height);
    this.context.strokeStyle = '#17365d';
    this.context.lineWidth = 2.4;
    this.context.lineCap = 'round';
    this.context.lineJoin = 'round';

    for (const stroke of this.strokes) {
      if (stroke.length === 0) {
        continue;
      }

      this.context.beginPath();
      this.context.moveTo(stroke[0].x * bounds.width, stroke[0].y * bounds.height);
      if (stroke.length === 1) {
        this.context.lineTo(stroke[0].x * bounds.width + 0.1, stroke[0].y * bounds.height + 0.1);
      } else {
        for (let index = 1; index < stroke.length; index++) {
          const previous = stroke[index - 1];
          const point = stroke[index];
          const midpointX = ((previous.x + point.x) / 2) * bounds.width;
          const midpointY = ((previous.y + point.y) / 2) * bounds.height;
          this.context.quadraticCurveTo(
            previous.x * bounds.width,
            previous.y * bounds.height,
            midpointX,
            midpointY
          );
        }

        const lastPoint = stroke[stroke.length - 1];
        this.context.lineTo(lastPoint.x * bounds.width, lastPoint.y * bounds.height);
      }
      this.context.stroke();
    }
  }

  private isSupportedFile(file: File): boolean {
    const extension = file.name.split('.').pop()?.toLowerCase();
    const documentExtensions = ['pdf', 'doc', 'docx'];
    const imageExtensions = ['bmp', 'gif', 'heic', 'heif', 'jpeg', 'jpg', 'png', 'svg', 'tif', 'tiff', 'webp'];
    return documentExtensions.includes(extension ?? '')
      || imageExtensions.includes(extension ?? '')
      || file.type.startsWith('image/');
  }

  private setStatus(message: string, type: 'error' | 'success'): void {
    this.statusMessage = message;
    this.statusType = type;
  }
}
