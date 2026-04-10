import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { ItineraryStore } from '../../../core/stores/itinerary.store';
import {
  AtmosphereType,
  BudgetTier,
  HotelType,
  UserPreferences
} from '../../../core/models/itinerary.models';

@Component({
  selector: 'app-itinerary-input',
  imports: [CommonModule, FormsModule],
  templateUrl: './itinerary-input.component.html',
  styleUrl: './itinerary-input.component.scss'
})
export class ItineraryInputComponent {
  private readonly api = inject(ApiService);
  private readonly store = inject(ItineraryStore);
  private readonly router = inject(Router);

  readonly loading = this.store.loading;
  readonly error = this.store.error;

  rawText = '';
  selectedFile = signal<File | null>(null);
  dragOver = signal(false);

  preferences: UserPreferences = {
    checkin: '',
    checkout: '',
    travelers: 2,
    budget_tier: 'mid',
    hotel_types: [],
    avoid_long_walking: false,
    must_be_near_station: null,
    preferred_atmosphere: []
  };

  readonly budgetOptions: { value: BudgetTier; label: string }[] = [
    { value: 'budget', label: 'Budget (¥5,000–¥10,000/night)' },
    { value: 'mid', label: 'Mid-range (¥10,000–¥25,000/night)' },
    { value: 'luxury', label: 'Luxury (¥25,000+/night)' }
  ];

  readonly atmosphereOptions: { value: AtmosphereType; label: string }[] = [
    { value: 'quiet', label: 'Quiet' },
    { value: 'historic', label: 'Historic' },
    { value: 'modern', label: 'Modern' },
    { value: 'shopping', label: 'Shopping' },
    { value: 'nightlife', label: 'Nightlife' },
    { value: 'family_friendly', label: 'Family-Friendly' }
  ];

  get canSubmit(): boolean {
    const hasInput = this.rawText.trim().length > 0 || this.selectedFile() !== null;
    return hasInput && !this.loading();
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.dragOver.set(true);
  }

  onDragLeave(): void {
    this.dragOver.set(false);
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.dragOver.set(false);
    const file = event.dataTransfer?.files[0];
    if (file) this.setFile(file);
  }

  onFileSelect(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (file) this.setFile(file);
  }

  clearFile(): void {
    this.selectedFile.set(null);
  }

  toggleAtmosphere(value: AtmosphereType): void {
    const current = this.preferences.preferred_atmosphere;
    this.preferences.preferred_atmosphere = current.includes(value)
      ? current.filter(a => a !== value)
      : [...current, value];
  }

  async submit(): Promise<void> {
    if (!this.canSubmit) return;

    this.store.setError(null);
    this.store.setLoading(true);
    this.store.updatePreferences(this.preferences);

    const file = this.selectedFile();
    const obs = file ? this.api.parseFile(file) : this.api.parseText(this.rawText);

    obs.subscribe({
      next: itinerary => {
        this.store.setItinerary(itinerary);
        this.store.setLoading(false);
        this.router.navigate(['/review']);
      },
      error: err => {
        const detail = err?.error?.detail ?? 'Something went wrong. Please try again.';
        this.store.setError(detail);
        this.store.setLoading(false);
      }
    });
  }

  private setFile(file: File): void {
    const allowed = ['.pdf', '.docx', '.txt'];
    const ext = file.name.substring(file.name.lastIndexOf('.')).toLowerCase();
    if (!allowed.includes(ext)) {
      this.store.setError('Unsupported file type. Please upload a PDF, DOCX, or TXT file.');
      return;
    }
    this.store.setError(null);
    this.selectedFile.set(file);
    this.rawText = '';
  }
}
