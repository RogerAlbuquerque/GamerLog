"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { Button } from "@/components/ui/Button";
import { Input, Select, Textarea } from "@/components/ui/Field";
import { Modal } from "@/components/ui/Modal";
import { collectionsApi } from "@/lib/api/endpoints";
import type { CollectionDetail } from "@/lib/api/types";
import { applyServerError } from "@/lib/forms";
import { collectionSchema, type CollectionForm as Values } from "@/lib/schemas";
import { toast } from "@/stores/toast";

const DEFAULTS: Values = { name: "", description: "", visibility: "Public" };

/** Create (no `existing`) or edit a collection's name, description and visibility. */
export function CollectionForm({ open, existing, onClose, onSaved }: { open: boolean; existing?: CollectionDetail["collection"]; onClose: () => void; onSaved: (c: CollectionDetail) => void }) {
  const [formError, setFormError] = useState<string | null>(null);
  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<Values>({ resolver: zodResolver(collectionSchema), defaultValues: DEFAULTS });

  useEffect(() => {
    if (open) {
      setFormError(null);
      reset(existing ? { name: existing.name, description: existing.description ?? "", visibility: existing.visibility } : DEFAULTS);
    }
  }, [open, existing, reset]);

  const onSubmit = handleSubmit(async (v) => {
    setFormError(null);
    const body = { name: v.name, description: v.description.trim() || null, visibility: v.visibility };
    try {
      const saved = existing ? await collectionsApi.update(existing.id, body) : await collectionsApi.create(body);
      toast.success(existing ? "Collection updated." : "Collection created.");
      onSaved(saved);
      onClose();
    } catch (error) {
      setFormError(applyServerError(error, setError, ["name", "description"]));
    }
  });

  return (
    <Modal open={open} onClose={onClose} title={existing ? "Edit collection" : "New collection"}>
      <form onSubmit={onSubmit} noValidate className="space-y-4">
        {formError && <p role="alert" className="rounded-xl border border-danger/40 bg-danger/10 px-4 py-3 text-sm text-danger">{formError}</p>}
        <Input label="Name" maxLength={100} autoFocus error={errors.name?.message} {...register("name")} />
        <Textarea label="Description (optional)" rows={3} maxLength={1000} error={errors.description?.message} {...register("description")} />
        <Select label="Who can see it?" hint="Private collections are visible only to you." {...register("visibility")}>
          <option value="Public">Public</option>
          <option value="Private">Private</option>
        </Select>
        <div className="flex justify-end gap-3">
          <Button variant="ghost" onClick={onClose}>Cancel</Button>
          <Button type="submit" loading={isSubmitting}>{existing ? "Save" : "Create collection"}</Button>
        </div>
      </form>
    </Modal>
  );
}
