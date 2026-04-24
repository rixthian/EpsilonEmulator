export class ProgressTracker {
  constructor(documentRef = document) {
    this.progressBar = documentRef.getElementById("progress-bar");
    this.progressLabel = documentRef.getElementById("progress-label");
  }

  render(snapshot) {
    if (this.progressBar) {
      this.progressBar.style.width = `${Math.max(0, Math.min(100, snapshot.progress || 0))}%`;
    }

    if (this.progressLabel) {
      this.progressLabel.textContent = snapshot.copy || "Preparando loader.";
    }
  }
}
