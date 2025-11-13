import { MessageCircle, RefreshCcw, Send, Sparkles, ThumbsDown, ThumbsUp } from 'lucide-react';
import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
	apiService,
	type RecommendationAssistantChatRequestDto,
	type RecommendationAssistantRequestDto,
	type RecommendationConversationMessageDto,
	type RecommendationDto,
	type RecommendationFeedbackDto,
	type RecommendationFeedbackSummaryDto
} from '../../services/apiService';
import './RecommendationAssistant.css';

type FeedbackState = 'idle' | 'sending' | 'sent' | 'error';

type Filters = {
	currentMood?: number;
	externalContext?: string;
};

type ConversationRole = 'user' | 'assistant';

type ConversationMessage = {
	id: string;
	role: ConversationRole;
	content: string;
	createdAt: number;
};

const buildFeedbackPayload = (
	recommendationId: string,
	accepted: boolean,
	extra?: Partial<RecommendationFeedbackDto>
): RecommendationFeedbackDto => ({
	recommendationId,
	accepted,
	notes: extra?.notes,
	moodAfter: extra?.moodAfter,
	submittedAtUtc: extra?.submittedAtUtc
});

const formatDateTime = (isoDate: string | Date): string => {
	const date = typeof isoDate === 'string' ? new Date(isoDate) : new Date(isoDate);

	if (Number.isNaN(date.getTime())) {
		return 'Sin horario sugerido';
	}

	return new Intl.DateTimeFormat('es-AR', {
		weekday: 'short',
		day: '2-digit',
		month: 'short',
		hour: '2-digit',
		minute: '2-digit'
	}).format(date);
};

const createMessageId = () => `${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 8)}`;
const CONTEXT_MESSAGE_ID = 'conversation-context';

const RecommendationAssistant: React.FC = () => {
	const [filters, setFilters] = useState<Filters>({});
	const [isLoading, setIsLoading] = useState<boolean>(false);
	const [recommendations, setRecommendations] = useState<RecommendationDto[]>([]);
	const [fallbackRecommendations, setFallbackRecommendations] = useState<RecommendationDto[]>([]);
	const [shouldShowRecommendations, setShouldShowRecommendations] = useState(false);
	const [feedbackStatus, setFeedbackStatus] = useState<Record<string, FeedbackState>>({});
	const [errorMessage, setErrorMessage] = useState<string | null>(null);
	const [summary, setSummary] = useState<RecommendationFeedbackSummaryDto | null>(null);
	const [summaryError, setSummaryError] = useState<string | null>(null);
	const [recommendationsInfo, setRecommendationsInfo] = useState<string | null>(null);
	const [conversation, setConversation] = useState<ConversationMessage[]>([
		{
			id: createMessageId(),
			role: 'assistant',
			content: '¡Hola! Soy Aurora, tu asistente personal. Contame cómo venís hoy y vemos juntos qué ajustes te sirven.',
			createdAt: Date.now()
		}
	]);
	const [chatInput, setChatInput] = useState('');
	const [isChatLoading, setIsChatLoading] = useState(false);
	const [chatError, setChatError] = useState<string | null>(null);
	const chatThreadRef = useRef<HTMLDivElement | null>(null);
	const chatInputRef = useRef<HTMLTextAreaElement | null>(null);

	const isContextProvided = useMemo(() => Boolean(filters.externalContext?.trim()), [filters.externalContext]);

	const contextSummary = useMemo(() => {
		const parts: string[] = [];

		if (filters.externalContext?.trim()) {
			parts.push(filters.externalContext.trim());
		}

		if (typeof filters.currentMood === 'number') {
			parts.push(`Ánimo: ${filters.currentMood}/5`);
		}

		return parts.join(' · ');
	}, [filters.currentMood, filters.externalContext]);

	const activeFiltersLabel = useMemo(() => {
		if (!filters.currentMood && !filters.externalContext) {
			return 'Sin filtros adicionales';
		}

		const parts: string[] = [];

		if (filters.currentMood) {
			parts.push(`Ánimo: ${filters.currentMood}/5`);
		}

		if (filters.externalContext) {
			parts.push(`Contexto: ${filters.externalContext}`);
		}

		return parts.join(' · ');
	}, [filters]);

	const loadRecommendations = useCallback(async (options?: { display?: boolean }) => {
		const display = options?.display ?? false;
		setIsLoading(true);
		setErrorMessage(null);
		if (display) {
			setRecommendationsInfo(null);
		}

		try {
			const payload = {
				referenceDate: new Date().toISOString(),
				limit: 5,
				currentMood: filters.currentMood,
				externalContext: filters.externalContext?.trim() || undefined
			};

			const result = await apiService.getRecommendations(payload);
			setFallbackRecommendations(result);
			setRecommendations(display ? result : []);
			return true;
		} catch (error) {
			const message =
				error instanceof Error ? error.message : 'No pudimos cargar las recomendaciones.';
			setErrorMessage(message);
			return false;
		} finally {
			setIsLoading(false);
		}
	}, [filters.currentMood, filters.externalContext]);

	const loadFeedbackSummary = useCallback(async (days = 30) => {
		try {
			setSummaryError(null);
			const result = await apiService.getRecommendationFeedbackSummary(days);
			setSummary(result);
		} catch (error) {
			const message = error instanceof Error ? error.message : 'No pudimos recuperar tu historial.';
			setSummaryError(message);
		}
	}, []);

	useEffect(() => {
		void loadRecommendations();
		void loadFeedbackSummary();
	}, [loadFeedbackSummary, loadRecommendations]);

	const scrollChatToBottom = useCallback(() => {
		if (!chatThreadRef.current) {
			return;
		}

		chatThreadRef.current.scrollTop = chatThreadRef.current.scrollHeight;
	}, []);

	useEffect(() => {
		scrollChatToBottom();
	}, [conversation, scrollChatToBottom]);

	useEffect(() => {
		setConversation((previousConversation) => {
			const existingContextMessage = previousConversation.find((message) => message.id === CONTEXT_MESSAGE_ID);
			const conversationWithoutContext = previousConversation.filter((message) => message.id !== CONTEXT_MESSAGE_ID);

			if (!isContextProvided) {
				if (!existingContextMessage) {
					return previousConversation;
				}

				return conversationWithoutContext;
			}

			const formattedContent = `Contexto actual: ${contextSummary}`;

			if (existingContextMessage && existingContextMessage.content === formattedContent) {
				return previousConversation;
			}

			const contextMessage: ConversationMessage = {
				id: CONTEXT_MESSAGE_ID,
				role: 'user',
				content: formattedContent,
				createdAt: existingContextMessage?.createdAt ?? Date.now()
			};

			const [firstMessage, ...rest] = conversationWithoutContext;

			if (firstMessage?.role === 'assistant') {
				return [firstMessage, contextMessage, ...rest];
			}

			return [contextMessage, ...conversationWithoutContext];
		});
	}, [contextSummary, isContextProvided]);

	const generateRecommendationsFromAi = useCallback(
		async (conversationMessages: ConversationMessage[]) => {
			setIsLoading(true);
			setErrorMessage(null);
			setRecommendationsInfo(null);

			try {
				const payload: RecommendationAssistantRequestDto = {
					conversation: conversationMessages.map<RecommendationConversationMessageDto>((message) => ({
						role: message.role,
						content: message.content
					})),
					currentMood: filters.currentMood,
					externalContext: filters.externalContext?.trim() || undefined,
					fallbackRecommendations
				};

				const generated = await apiService.generateConversationalRecommendations(payload);

				if (generated.length === 0) {
					throw new Error('La IA no generó recomendaciones útiles.');
				}

				const heuristicsLimit = Math.min(fallbackRecommendations.length, Math.max(0, 5 - 1));
				const heuristicsBase = fallbackRecommendations
					.slice(0, heuristicsLimit > 0 ? heuristicsLimit : fallbackRecommendations.length)
					.map((item) => ({ ...item }));

				const timestamp = Date.now();
				const aiCandidates = generated.map((item, index) => ({
					...item,
					recommendationType: item.recommendationType?.trim().toLowerCase() === 'ai' ? item.recommendationType : 'ai',
					id: item.id && item.id.trim().length > 0 ? item.id : `ai-${timestamp}-${index}`
				}));

				const aiFeatured = aiCandidates[0];
				let combined: RecommendationDto[];

				if (aiFeatured) {
					const duplicateIndex = heuristicsBase.findIndex((item) => item.id === aiFeatured.id);
					const safeAi = duplicateIndex >= 0 ? { ...aiFeatured, id: `ai-${timestamp}` } : aiFeatured;
					combined = [safeAi, ...heuristicsBase];
				} else {
					combined = heuristicsBase;
				}

				if (combined.length === 0) {
					combined = aiCandidates;
				}

				setRecommendations(combined);
				setShouldShowRecommendations(true);
				setRecommendationsInfo('Sumamos una recomendación IA junto a las sugerencias base.');
			} catch (error) {
				console.error('No se pudieron generar recomendaciones con IA', error);
				setShouldShowRecommendations(true);
				const fallbackLoaded = await loadRecommendations({ display: true });
				if (fallbackLoaded) {
					setRecommendationsInfo('No pudimos usar la IA, mostramos las sugerencias base.');
				}
			} finally {
				setIsLoading(false);
			}
		},
		[fallbackRecommendations, filters.currentMood, filters.externalContext, loadRecommendations]
	);

	const handleSendChat = async (
		event?: React.FormEvent<HTMLFormElement> | React.MouseEvent<HTMLButtonElement>
	) => {
		if (event) {
			event.preventDefault();
		}

		const trimmed = chatInput.trim();
		if (!trimmed || isChatLoading || !isContextProvided) {
			return;
		}

		const userMessage: ConversationMessage = {
			id: createMessageId(),
			role: 'user',
			content: trimmed,
			createdAt: Date.now()
		};

		const userConversation = [...conversation, userMessage];

		setConversation(userConversation);
		setChatInput('');
		setChatError(null);
		setIsChatLoading(true);

		try {
			const payload: RecommendationAssistantChatRequestDto = {
				conversation: userConversation.map((message) => ({
					role: message.role,
					content: message.content
				})),
				currentMood: filters.currentMood,
				externalContext: filters.externalContext?.trim() || undefined
			};

			const response = await apiService.generateAssistantReply(payload);
			const responseText = response.message;

			const assistantMessage: ConversationMessage = {
				id: createMessageId(),
				role: 'assistant',
				content: responseText,
				createdAt: Date.now()
			};

			const finalConversation = [...userConversation, assistantMessage];
			setConversation(finalConversation);

			await generateRecommendationsFromAi(finalConversation);
		} catch (error) {
			const message = error instanceof Error ? error.message : 'No pudimos conectar con el asistente.';
			setChatError(message);
		} finally {
			setIsChatLoading(false);
			chatInputRef.current?.focus();
		}
	};

	const handleQuickPrompt = (prompt: string) => {
		if (!isContextProvided) {
			return;
		}

		setChatInput(prompt);
		chatInputRef.current?.focus();
	};

	const quickPrompts = useMemo(
		() => [
			'Necesito organizarme sin estrés, ¿por dónde arranco?',
			'¿Qué actividad corta me puede ayudar a recargar energías?',
			'Recordame algo que me motive para seguir con mis planes.'
		],
		[]
	);

	const handleMoodChange = (event: React.ChangeEvent<HTMLSelectElement>) => {
		const value = event.target.value;
		setFilters((prev) => ({
			...prev,
			currentMood: value === '' ? undefined : Number.parseInt(value, 10)
		}));
	};

	const handleContextChange = (event: React.ChangeEvent<HTMLInputElement>) => {
		const value = event.target.value;
		setFilters((prev) => ({
			...prev,
			externalContext: value
		}));
	};

	const handleClearFilters = () => {
		setFilters({});
	};

	const sendFeedback = async (recommendationId: string, accepted: boolean) => {
		setFeedbackStatus((prev) => ({
			...prev,
			[recommendationId]: 'sending'
		}));

		try {
			const payload = buildFeedbackPayload(recommendationId, accepted);
			await apiService.submitRecommendationFeedback(payload);

			setFeedbackStatus((prev) => ({
				...prev,
				[recommendationId]: 'sent'
			}));

			void loadFeedbackSummary();
		} catch (error) {
			console.error('Error enviando feedback', error);
			setFeedbackStatus((prev) => ({
				...prev,
				[recommendationId]: 'error'
			}));
		}
	};

	const renderContent = () => {
		if (!shouldShowRecommendations) {
			return (
				<div className="recommendations-prompt" role="status">
					<p>Chateá con el asistente y generaremos sugerencias a medida.</p>
				</div>
			);
		}

		if (isLoading) {
			return (
				<div className="recommendations-loading" data-testid="recommendations-loading">
					{Array.from({ length: 3 }).map((_, index) => (
						<div key={index} className="recommendation-skeleton">
							<div className="skeleton shimmer" />
						</div>
					))}
				</div>
			);
		}

		if (errorMessage) {
			return (
				<div className="recommendations-error" role="alert">
					<p>{errorMessage}</p>
					<button type="button" onClick={() => void loadRecommendations({ display: true })}>
						Reintentar
					</button>
				</div>
			);
		}

		if (recommendations.length === 0) {
			return (
				<div className="recommendations-empty" role="status">
					<p>No encontramos sugerencias en este momento.</p>
					<button type="button" onClick={() => void loadRecommendations({ display: true })}>
						Volver a intentar
					</button>
				</div>
			);
		}

		return (
			<>
				{recommendationsInfo && (
					<p className="recommendations-source" role="status">{recommendationsInfo}</p>
				)}
				<div className="recommendations-grid">
					{recommendations.map((item) => {
						const status = feedbackStatus[item.id] ?? 'idle';
						const isSending = status === 'sending';
						const isSent = status === 'sent';
						const isError = status === 'error';
						const isAiRecommendation = item.recommendationType?.toLowerCase() === 'ai';

						return (
							<article key={item.id} className="recommendation-card">
								<header className="recommendation-card-header">
									<div className="recommendation-card-title">
										<Sparkles aria-hidden="true" size={18} />
										<div>
											<h3>{item.title}</h3>
											{item.subtitle && <p className="recommendation-subtitle">{item.subtitle}</p>}
										</div>
									</div>
									<div className="recommendation-card-badges">
										{isAiRecommendation && (
											<span className="recommendation-badge is-ai">IA</span>
										)}
										<span className="recommendation-badge">{Math.round(item.confidence * 100)}% match</span>
									</div>
								</header>

								<p className="recommendation-reason">{item.reason}</p>

								<dl className="recommendation-meta">
									<div>
										<dt>Horario sugerido</dt>
										<dd>{formatDateTime(item.suggestedStart)}</dd>
									</div>
									<div>
										<dt>Duración</dt>
										<dd>{item.suggestedDurationMinutes} minutos</dd>
									</div>
									{item.categoryName && (
										<div>
											<dt>Categoría</dt>
											<dd>{item.categoryName}</dd>
										</div>
									)}
									{item.moodImpact && (
										<div>
											<dt>Impacto esperado</dt>
											<dd>{item.moodImpact}</dd>
										</div>
									)}
									{item.summary && (
										<div>
											<dt>Resumen</dt>
											<dd>{item.summary}</dd>
										</div>
									)}
								</dl>

								<footer className="recommendation-actions">
									<button
										type="button"
										className="action-button is-positive"
										disabled={isSending || isSent}
										onClick={() => void sendFeedback(item.id, true)}
									>
										<ThumbsUp size={16} aria-hidden="true" />
										{isSent ? '¡Gracias!' : 'Me sirve'}
									</button>
									<button
										type="button"
										className="action-button is-negative"
										disabled={isSending || isSent}
										onClick={() => void sendFeedback(item.id, false)}
									>
										<ThumbsDown size={16} aria-hidden="true" />
										Pasar
									</button>
								</footer>

								{isError && (
									<p className="recommendation-feedback-error" role="status">
										No pudimos guardar tu feedback. Probá de nuevo.
									</p>
								)}
							</article>
						);
					})}
				</div>
			</>
		);
	};

	return (
		<section className="recommendation-assistant" aria-labelledby="recommendation-assistant-title">
			<header className="assistant-header">
				<div>
					<h2 id="recommendation-assistant-title">Asistente de recomendaciones</h2>
					<p>Recomendaciones personalizadas para organizar tu día.</p>
				</div>
				<button
					type="button"
					className="refresh-button"
					onClick={() => void loadRecommendations({ display: shouldShowRecommendations })}
					disabled={isLoading}
				>
					<RefreshCcw size={16} aria-hidden="true" />
					Actualizar
				</button>
			</header>

			<form
				className="assistant-filters"
				onSubmit={(event) => {
					event.preventDefault();
					void loadRecommendations({ display: shouldShowRecommendations });
				}}
			>
				<div className="filter-group">
					<label htmlFor="mood-filter">¿Cómo te sentís hoy?</label>
					<select
						id="mood-filter"
						value={filters.currentMood?.toString() ?? ''}
						onChange={handleMoodChange}
					>
						<option value="">Sin filtro</option>
						<option value="5">5 - Energía total</option>
						<option value="4">4 - Muy bien</option>
						<option value="3">3 - Neutral</option>
						<option value="2">2 - Bajo</option>
						<option value="1">1 - Necesito calma</option>
					</select>
				</div>

				<div className="filter-group">
					<label htmlFor="context-filter">Contexto (opcional)</label>
					<input
						id="context-filter"
						type="text"
						placeholder="Ej: Lluvioso, trabajo remoto, sin gimnasio"
						value={filters.externalContext ?? ''}
						onChange={handleContextChange}
					/>
				</div>

				<div className="filters-actions">
					<span className="filters-summary">{activeFiltersLabel}</span>
					<div className="filters-buttons">
						<button type="submit" className="apply-button" disabled={isLoading}>
							Ver sugerencias
						</button>
						<button type="button" className="clear-button" onClick={handleClearFilters} disabled={isLoading}>
							Limpiar
						</button>
					</div>
				</div>
			</form>

			<section className="assistant-chat" aria-label="Conversación con el asistente">
				<header className="assistant-chat-header">
					<div className="assistant-chat-title">
						<MessageCircle size={18} aria-hidden="true" />
						<h3>Hablemos de tu día</h3>
					</div>
					{isChatLoading && <span className="assistant-chat-status">Pensando…</span>}
				</header>

				<div className="assistant-chat-thread" ref={chatThreadRef}>
					{conversation.map((message) => (
						<article
							key={message.id}
							className={`assistant-chat-bubble assistant-chat-bubble-${message.role}`}
						>
							<p>{message.content}</p>
						</article>
					))}
				</div>

				{chatError && (
					<p className="assistant-chat-error" role="alert">{chatError}</p>
				)}

				{!isContextProvided && (
					<p className="assistant-chat-error" role="status">
						Sumá un contexto en los filtros para que podamos conversar.
					</p>
				)}

				<form className="assistant-chat-form" onSubmit={handleSendChat}>
					<textarea
						ref={chatInputRef}
						placeholder="Contame qué necesitas y buscamos una solución juntos"
						value={chatInput}
						onChange={(event) => setChatInput(event.target.value)}
						disabled={isChatLoading || !isContextProvided}
						rows={3}
					/>
					<div className="assistant-chat-actions">
						<div className="assistant-chat-prompts">
							{quickPrompts.map((prompt) => (
								<button
									key={prompt}
									type="button"
									onClick={() => handleQuickPrompt(prompt)}
									disabled={isChatLoading || !isContextProvided}
								>
									{prompt}
								</button>
							))}
						</div>
						<button
							type="submit"
							className="assistant-chat-send"
							disabled={isChatLoading || chatInput.trim().length === 0 || !isContextProvided}
						>
							{isChatLoading ? (
								'Enviando…'
							) : (
								<>
									<Send size={16} aria-hidden="true" />
									<span>Enviar</span>
								</>
							)}
						</button>
					</div>
				</form>
			</section>

			{renderContent()}

			<aside className="assistant-summary" aria-label="Resumen de feedback">
				<header className="assistant-summary-header">
					<h3>Tu interacción con el asistente</h3>
					<button type="button" onClick={() => void loadFeedbackSummary()}>
						Actualizar
					</button>
				</header>

				{summaryError && (
					<p className="assistant-summary-error" role="status">{summaryError}</p>
				)}

				{summary && (
					<div className="assistant-summary-grid">
						<div>
							<span className="assistant-summary-label">Feedback total</span>
							<span className="assistant-summary-value">{summary.totalFeedback}</span>
						</div>
						<div>
							<span className="assistant-summary-label">Aceptadas</span>
							<span className="assistant-summary-value">{summary.acceptedCount}</span>
						</div>
						<div>
							<span className="assistant-summary-label">Tasa de acierto</span>
							<span className="assistant-summary-value">{summary.acceptanceRate.toFixed(1)}%</span>
						</div>
						<div>
							<span className="assistant-summary-label">Ánimo post recomendación</span>
							<span className="assistant-summary-value">{summary.averageMoodAfter?.toFixed(1) ?? 'Sin datos'}</span>
						</div>
					</div>
				)}
			</aside>
		</section>
	);
};

export default RecommendationAssistant;

