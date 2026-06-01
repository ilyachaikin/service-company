import React, { useEffect, useState } from 'react';
import { Modal, Form, Input, Switch, message, Select, DatePicker, Row, Col } from 'antd';
import api from '../../api/client';
import dayjs from 'dayjs';

interface EquipmentFormProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  initialData?: any;
}

const EquipmentForm: React.FC<EquipmentFormProps> = ({ isOpen, onClose, onSuccess, initialData }) => {
  const [form] = Form.useForm();
  const [objects, setObjects] = useState<any[]>([]);

  useEffect(() => {
    if (isOpen) {
      fetchObjects();
      if (initialData) {
        form.setFieldsValue({
          ...initialData,
          purchaseDate: initialData.purchaseDate ? dayjs(initialData.purchaseDate) : null,
          warrantyExpiryDate: initialData.warrantyExpiryDate ? dayjs(initialData.warrantyExpiryDate) : null,
        });
      } else {
        form.resetFields();
      }
    }
  }, [isOpen, initialData, form]);

  const fetchObjects = async () => {
    const res = await api.get('/ServiceObjects', { params: { pageSize: 200 } });
    setObjects(res.data.items);
  };

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();
      const payload = {
        ...values,
        purchaseDate: values.purchaseDate ? values.purchaseDate.toISOString() : null,
        warrantyExpiryDate: values.warrantyExpiryDate ? values.warrantyExpiryDate.toISOString() : null,
      };

      if (initialData) {
        await api.put(`/Equipments/${initialData.id}`, { ...payload, id: initialData.id });
        message.success('Оборудование обновлено');
      } else {
        await api.post('/Equipments', payload);
        message.success('Оборудование добавлено');
      }
      onSuccess();
    } catch (error: any) {
      message.error(error.response?.data?.error || 'Ошибка валидации');
    }
  };

  return (
    <Modal
      title={initialData ? 'Редактировать оборудование' : 'Добавить оборудование'}
      open={isOpen}
      onCancel={onClose}
      onOk={handleSubmit}
      okText={initialData ? 'Сохранить' : 'Создать'}
      cancelText="Отмена"
      width={700}
    >
      <Form form={form} layout="vertical" initialValues={{ isActive: true, status: 0 }}>
        <Row gutter={16}>
          <Col span={24}>
            <Form.Item name="serviceObjectId" label="Объект (место расположения)" rules={[{ required: true, message: 'Выберите объект' }]}>
              <Select placeholder="Выберите объект" disabled={!!initialData}>
                {objects.map(o => (
                  <Select.Option key={o.id} value={o.id}>
                    {o.name} ({o.clientName})
                  </Select.Option>
                ))}
              </Select>
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item name="name" label="Наименование" rules={[{ required: true, message: 'Введите наименование' }]}>
              <Input placeholder="Например: Кондиционер Mitsubishi 1" />
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item name="serialNumber" label="Серийный номер">
              <Input placeholder="SN-XXXX-XXXX" />
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item name="model" label="Модель">
              <Input placeholder="Например: Carrier 50JX" />
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item name="manufacturer" label="Производитель">
              <Input placeholder="Например: Carrier" />
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item name="purchaseDate" label="Дата покупки">
              <DatePicker style={{ width: '100%' }} />
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item name="warrantyExpiryDate" label="Гарантия до">
              <DatePicker style={{ width: '100%' }} />
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item name="status" label="Статус" rules={[{ required: true, message: 'Выберите статус' }]}>
              <Select>
                <Select.Option value={0}>Работает</Select.Option>
                <Select.Option value={1}>Неисправно</Select.Option>
                <Select.Option value={2}>На обслуживании</Select.Option>
                <Select.Option value={3}>Списано</Select.Option>
              </Select>
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item name="isActive" label="Активно" valuePropName="checked">
              <Switch />
            </Form.Item>
          </Col>
        </Row>
      </Form>
    </Modal>
  );
};

export default EquipmentForm;
