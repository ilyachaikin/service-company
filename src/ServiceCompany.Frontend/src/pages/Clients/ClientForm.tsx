import React, { useEffect } from 'react';
import { Modal, Form, Input, Switch, message } from 'antd';
import api from '../../api/client';

interface ClientFormProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  initialData?: any;
}

const ClientForm: React.FC<ClientFormProps> = ({ isOpen, onClose, onSuccess, initialData }) => {
  const [form] = Form.useForm();

  useEffect(() => {
    if (isOpen) {
      if (initialData) {
        form.setFieldsValue(initialData);
      } else {
        form.resetFields();
      }
    }
  }, [isOpen, initialData, form]);

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();
      if (initialData) {
        await api.put(`/Clients/${initialData.id}`, { ...values, id: initialData.id });
        message.success('Клиент обновлён');
      } else {
        await api.post('/Clients', values);
        message.success('Клиент создан');
      }
      onSuccess();
    } catch (error: any) {
      message.error(error.response?.data?.error || 'Ошибка валидации');
    }
  };

  return (
    <Modal
      title={initialData ? 'Редактировать клиента' : 'Добавить клиента'}
      open={isOpen}
      onCancel={onClose}
      onOk={handleSubmit}
      okText={initialData ? 'Сохранить' : 'Создать'}
      cancelText="Отмена"
      width={600}
    >
      <Form form={form} layout="vertical" initialValues={{ isActive: true }}>
        <Form.Item name="name" label="Наименование организации" rules={[{ required: true, message: 'Введите наименование организации' }]}>
          <Input placeholder="Например: ООО «РосТех»" />
        </Form.Item>
        <Form.Item
          name="inn"
          label="ИНН"
          rules={[
            { required: true, message: 'Введите ИНН' },
            { pattern: /^\d{10}(\d{2})?$/, message: 'ИНН должен содержать 10 или 12 цифр' }
          ]}
        >
          <Input placeholder="10 или 12 цифр" />
        </Form.Item>
        <Form.Item name="email" label="Email" rules={[{ type: 'email', message: 'Неверный формат email' }]}>
          <Input placeholder="info@company.ru" />
        </Form.Item>
        <Form.Item name="phoneNumber" label="Телефон">
          <Input placeholder="+7 (xxx) xxx-xx-xx" />
        </Form.Item>
        <Form.Item name="address" label="Юридический адрес">
          <Input.TextArea rows={2} placeholder="Юридический адрес" />
        </Form.Item>
        <Form.Item name="isActive" label="Активен" valuePropName="checked">
          <Switch />
        </Form.Item>
      </Form>
    </Modal>
  );
};

export default ClientForm;
