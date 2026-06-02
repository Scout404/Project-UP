import React, { useEffect, useMemo, useState } from 'react';
import './LoginModal.css';
import './ProductDetailModal.css';
import { apiUrl } from './api';

function Stars({ rating, label }) {
  const roundedRating = Math.round(Number(rating) || 0);

  return (
    <span className="stars" aria-label={label || `${roundedRating} out of 5 stars`}>
      {[1, 2, 3, 4, 5].map((star) => (
        <span
          key={star}
          className={star <= roundedRating ? 'star filled' : 'star empty'}
          aria-hidden="true"
        >
          {star <= roundedRating ? '★' : '☆'}
        </span>
      ))}
    </span>
  );
}

function resolveImageUrl(product) {
  const imageUrl = product?.imageUrl || product?.image || product?.productImage || product?.pictureUrl;

  if (!imageUrl) {
    return '';
  }

  if (/^(https?:|data:|blob:)/i.test(imageUrl)) {
    return imageUrl;
  }

  return apiUrl(imageUrl.startsWith('/') ? imageUrl : `/${imageUrl}`);
}

function ProductDetailModal({ product, isOpen, onClose, user }) {
  const [reviews, setReviews] = useState([]);
  const [loadingReviews, setLoadingReviews] = useState(false);
  const [reviewsError, setReviewsError] = useState('');
  const [reviewText, setReviewText] = useState('');
  const [reviewRating, setReviewRating] = useState(5);
  const [submittingReview, setSubmittingReview] = useState(false);
  const [reviewSubmitError, setReviewSubmitError] = useState('');
  const [deletingReviewId, setDeletingReviewId] = useState(null);

  const productImageUrl = resolveImageUrl(product);
  const isLoggedIn = Boolean(user && !user.isGuest && user.id);

  const averageRating = useMemo(() => {
    if (!reviews.length) {
      return 0;
    }

    const total = reviews.reduce((sum, review) => sum + (Number(review.rating) || 0), 0);
    return total / reviews.length;
  }, [reviews]);

  useEffect(() => {
    if (!isOpen) {
      return undefined;
    }

    const handleKeyDown = (event) => {
      if (event.key === 'Escape') {
        onClose();
      }
    };

    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [isOpen, onClose]);

  useEffect(() => {
    if (!isOpen || !product?.productId) {
      return undefined;
    }

    const controller = new AbortController();
    setLoadingReviews(true);
    setReviewsError('');

    fetch(apiUrl(`/products/${product.productId}/reviews`), {
      signal: controller.signal
    })
      .then((response) => {
        if (!response.ok) {
          throw new Error('Failed to load reviews');
        }

        return response.json();
      })
      .then(setReviews)
      .catch((error) => {
        if (error.name !== 'AbortError') {
          console.error('Failed to fetch product reviews:', error);
          setReviews([]);
          setReviewsError('Reviews konden niet worden geladen.');
        }
      })
      .finally(() => {
        if (!controller.signal.aborted) {
          setLoadingReviews(false);
        }
      });

    return () => controller.abort();
  }, [isOpen, product?.productId]);

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    setReviewText('');
    setReviewRating(5);
    setReviewSubmitError('');
    setDeletingReviewId(null);
  }, [isOpen, product?.productId]);

  const handleReviewSubmit = async (event) => {
    event.preventDefault();

    const trimmedReviewText = reviewText.trim();

    if (!trimmedReviewText) {
      setReviewSubmitError('Vul eerst je review in.');
      return;
    }

    setSubmittingReview(true);
    setReviewSubmitError('');

    try {
      const response = await fetch(apiUrl(`/products/${product.productId}/reviews`), {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          userId: Number(user.id),
          reviewText: trimmedReviewText,
          rating: Number(reviewRating)
        })
      });

      if (!response.ok) {
        const message = await response.text();
        throw new Error(message || 'Review kon niet worden geplaatst.');
      }

      const createdReview = await response.json();
      setReviews((currentReviews) => [
        createdReview,
        ...currentReviews.filter((review) => review.reviewId !== createdReview.reviewId)
      ]);
      setReviewText('');
      setReviewRating(5);
    } catch (error) {
      console.error('Failed to submit product review:', error);
      setReviewSubmitError('Review kon niet worden geplaatst.');
    } finally {
      setSubmittingReview(false);
    }
  };

  const handleReviewDelete = async (review) => {
    if (!isLoggedIn || Number(review.userId) !== Number(user.id)) {
      return;
    }

    setDeletingReviewId(review.reviewId);
    setReviewSubmitError('');

    try {
      const response = await fetch(
        apiUrl(`/products/${product.productId}/reviews/${review.reviewId}/users/${user.id}`),
        {
          method: 'DELETE'
        }
      );

      if (!response.ok) {
        const message = await response.text();
        throw new Error(message || 'Review kon niet worden verwijderd.');
      }

      setReviews((currentReviews) => (
        currentReviews.filter((currentReview) => currentReview.reviewId !== review.reviewId)
      ));
    } catch (error) {
      console.error('Failed to delete product review:', error);
      setReviewSubmitError('Review kon niet worden verwijderd.');
    } finally {
      setDeletingReviewId(null);
    }
  };

  if (!isOpen || !product) {
    return null;
  }

  return (
    <div className="login-modal-overlay product-detail-overlay" onClick={onClose}>
      <section
        className="product-detail-modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="product-detail-title"
        onClick={(event) => event.stopPropagation()}
      >
        <button className="modal-close" type="button" onClick={onClose} aria-label="Close product details">
          x
        </button>

        <div className="product-detail-layout">
          <div className="product-detail-media">
            {productImageUrl ? (
              <img src={productImageUrl} alt={product.name} />
            ) : (
              <div className="product-image-placeholder" aria-label="Product image coming soon">
                <span className="placeholder-icon" aria-hidden="true">□</span>
                <strong>Coming Soon</strong>
              </div>
            )}
          </div>

          <div className="product-detail-content">
            <div className="product-detail-heading">
              <p className="product-detail-brand">{product.brand || product.categoryName || 'Product'}</p>
              <h2 id="product-detail-title">{product.name}</h2>
              <p className="product-detail-price">€ {Number(product.basePrice || 0).toFixed(2)}</p>
            </div>

            <div className="product-detail-description">
              <h3>Beschrijving</h3>
              <p>{product.description || 'Geen beschrijving beschikbaar.'}</p>
            </div>

            <div className="reviews-section">
              <div className="reviews-summary">
                <div>
                  <h3>Reviews</h3>
                  <p>
                    {reviews.length
                      ? `${reviews.length} review${reviews.length === 1 ? '' : 's'}`
                      : 'Nog geen reviews'}
                  </p>
                </div>
                <div className="average-rating" aria-label={`Average rating ${averageRating.toFixed(1)} out of 5`}>
                  <strong>{averageRating.toFixed(1)}</strong>
                  <Stars rating={averageRating} label={`Average rating ${averageRating.toFixed(1)} out of 5 stars`} />
                </div>
              </div>

              {isLoggedIn ? (
                <form className="review-form" onSubmit={handleReviewSubmit}>
                  <div className="review-form-row">
                    <label htmlFor={`review-rating-${product.productId}`}>Rating</label>
                    <select
                      id={`review-rating-${product.productId}`}
                      value={reviewRating}
                      onChange={(event) => setReviewRating(event.target.value)}
                    >
                      {[5, 4, 3, 2, 1].map((rating) => (
                        <option key={rating} value={rating}>
                          {rating} / 5
                        </option>
                      ))}
                    </select>
                  </div>

                  <label htmlFor={`review-text-${product.productId}`}>Je review</label>
                  <textarea
                    id={`review-text-${product.productId}`}
                    value={reviewText}
                    onChange={(event) => setReviewText(event.target.value)}
                    rows="3"
                    maxLength="1000"
                    placeholder="Wat vind je van dit product?"
                  />

                  {reviewSubmitError && <p className="review-submit-error">{reviewSubmitError}</p>}

                  <button type="submit" disabled={submittingReview}>
                    {submittingReview ? 'Plaatsen...' : 'Plaats review'}
                  </button>
                </form>
              ) : (
                <p className="reviews-state">Log in om een review te plaatsen.</p>
              )}

              {loadingReviews && <p className="reviews-state">Reviews laden...</p>}
              {reviewsError && <p className="reviews-state error">{reviewsError}</p>}

              {!loadingReviews && !reviewsError && (
                <div className="reviews-list">
                  {reviews.length === 0 ? (
                    <p className="reviews-state">Er zijn nog geen reviews voor dit product.</p>
                  ) : (
                    reviews.map((review) => {
                      const canDeleteReview = isLoggedIn && Number(review.userId) === Number(user.id);

                      return (
                      <article className="review-card" key={review.reviewId}>
                        <div className="review-header">
                          <div>
                            <strong>{review.reviewerName}</strong>
                            {review.createdAt && (
                              <time dateTime={review.createdAt}>
                                {new Date(review.createdAt).toLocaleDateString('nl-NL')}
                              </time>
                            )}
                          </div>
                          <div className="review-actions">
                            <Stars rating={review.rating} label={`${review.rating} out of 5 stars`} />
                            {canDeleteReview && (
                              <button
                                type="button"
                                className="review-delete-btn"
                                onClick={() => handleReviewDelete(review)}
                                disabled={deletingReviewId === review.reviewId}
                              >
                                {deletingReviewId === review.reviewId ? 'Verwijderen...' : 'Verwijder'}
                              </button>
                            )}
                          </div>
                        </div>
                        <p>{review.reviewText}</p>
                      </article>
                      );
                    })
                  )}
                </div>
              )}
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}

export default ProductDetailModal;
