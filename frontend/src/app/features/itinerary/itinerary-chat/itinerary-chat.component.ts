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
import { ChatItinerary, ChatMessage, ParsedItinerary } from '../../../core/models/itinerary.models';

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

  /** The current itinerary in camelCase format (from parent) */
  readonly currentItinerary = input<ChatItinerary | null>(null);

  /** Emitted when the user accepts an AI-suggested itinerary update */
  readonly itineraryAccepted = output<ParsedItinerary>();

  readonly messages = signal<ChatMessage[]>([{
    role: 'assistant',
    text: 'Hi! Ask me anything about your itinerary, or paste new itinerary text to re-parse it.',
    timestamp: new Date()
  }]);

  readonly inputText = signal('');
  readonly isSending = signal(false);
  readonly error = signal<string | null>(null);

  private shouldScrollToBottom = false;

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

    const sessionId = `chat-${Date.now()}`;
    this.api.sendChatMessage(sessionId, text, this.currentItinerary()).subscribe({
      next: response => {
        this.messages.update(msgs => [...msgs, {
          role: 'assistant',
          text: response.message,
          timestamp: new Date(),
          updatedItinerary: response.hasItineraryUpdate && response.updatedItinerary
            ? response.updatedItinerary
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

  acceptUpdate(chatItinerary: ChatItinerary): void {
    const parsed = this.toParsedItinerary(chatItinerary);
    this.store.setItinerary(parsed);
    this.itineraryAccepted.emit(parsed);
    this.messages.update(msgs => [...msgs, {
      role: 'assistant',
      text: 'Great — your itinerary has been updated. Review the changes above.',
      timestamp: new Date()
    }]);
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

  private toParsedItinerary(chat: ChatItinerary): ParsedItinerary {
    return {
      destinations: chat.destinations.map(d => ({
        name: d.name,
        raw_name: d.name,
        city: d.city,
        region: d.region,
        day_number: d.dayNumber,
        activity_type: d.activityType,
        geo_point: (d.lat != null && d.lng != null) ? { lat: d.lat, lng: d.lng } : null
      })),
      travel_dates: (chat.startDate && chat.endDate)
        ? { start: chat.startDate, end: chat.endDate }
        : null,
      raw_text_preview: chat.rawText?.slice(0, 300) ?? '',
      parsing_confidence: chat.parsingConfidence === 'high' ? 'high' : 'low',
      clarification_needed: chat.clarificationNeeded,
      is_multi_region: chat.isMultiRegion,
      regions_detected: chat.regionsDetected,
      parsed_by: 'ai'
    };
  }
}
