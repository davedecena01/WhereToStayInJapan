import {
  Component,
  ElementRef,
  inject,
  input,
  output,
  signal,
  ViewChild,
  AfterViewChecked
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { ItineraryStore } from '../../../core/stores/itinerary.store';
import { ChatItinerary, ChatMessage } from '../../../core/models/itinerary.models';

@Component({
  selector: 'app-itinerary-chat',
  imports: [CommonModule, FormsModule],
  templateUrl: './itinerary-chat.component.html',
  styleUrl: './itinerary-chat.component.scss'
})
export class ItineraryChatComponent implements AfterViewChecked {
  @ViewChild('messageList') private messageListRef!: ElementRef<HTMLElement>;

  private readonly api = inject(ApiService);
  private readonly store = inject(ItineraryStore);

  readonly currentItinerary = input<ChatItinerary | null>(null);

  /** Emitted when the user accepts an AI-suggested itinerary update */
  readonly itineraryAccepted = output<ChatItinerary>();

  readonly messages = signal<ChatMessage[]>([{
    role: 'assistant',
    text: 'Hi! Ask me anything about your itinerary, or paste new itinerary text to re-parse it.',
    timestamp: new Date()
  }]);

  readonly inputText = signal('');
  readonly isSending = signal(false);
  readonly error = signal<string | null>(null);

  private shouldScrollToBottom = false;
  private sessionId: string | null = null;

  ngAfterViewChecked(): void {
    if (this.shouldScrollToBottom) {
      this.scrollToBottom();
      this.shouldScrollToBottom = false;
    }
  }

  send(): void {
    const text = this.inputText().trim();
    if (!text || this.isSending()) return;

    this.messages.update(msgs => [...msgs, {
      role: 'user',
      text,
      timestamp: new Date()
    }]);
    this.inputText.set('');
    this.isSending.set(true);
    this.error.set(null);
    this.shouldScrollToBottom = true;

    if (!this.sessionId) this.sessionId = `chat-${Date.now()}`;
    this.api.sendChatMessage(this.sessionId, text, this.currentItinerary()).subscribe({
      next: response => {
        this.messages.update(msgs => [...msgs, {
          role: 'assistant',
          text: response.message,
          timestamp: new Date(),
          updatedItinerary: response.has_itinerary_update && response.updated_itinerary
            ? response.updated_itinerary
            : undefined
        }]);
        this.isSending.set(false);
        this.shouldScrollToBottom = true;
      },
      error: () => {
        this.error.set('Could not reach the AI assistant. Please try again.');
        this.isSending.set(false);
      }
    });
  }

  acceptUpdate(itinerary: ChatItinerary): void {
    this.store.setItinerary(itinerary);
    this.itineraryAccepted.emit(itinerary);
    this.messages.update(msgs => [...msgs, {
      role: 'assistant',
      text: 'Great — your itinerary has been updated. Review the changes above.',
      timestamp: new Date()
    }]);
  }

  onInput(event: Event): void {
    this.inputText.set((event.target as HTMLInputElement).value);
  }

  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.send();
    }
  }

  private scrollToBottom(): void {
    const el = this.messageListRef?.nativeElement;
    if (el) el.scrollTop = el.scrollHeight;
  }
}
