import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Card, Row, Col, Typography, Tag, Steps, Button, Descriptions, Divider,
  List, Input, Space, message, Select, Timeline, Badge, Tabs, Modal, Form,
  InputNumber, DatePicker, Alert, Tooltip,
} from 'antd';
import {
  UserOutlined, ClockCircleOutlined, CommentOutlined, CheckCircleOutlined,
  FileDoneOutlined, PlusOutlined, WarningOutlined, InboxOutlined,
  ReloadOutlined, PaperClipOutlined, DownloadOutlined, DeleteOutlined,
} from '@ant-design/icons';
import api from '../../api/client';
import dayjs from 'dayjs';

const { Title, Text, Paragraph } = Typography;

function parseApiErrors(err: any, fallback = 'Произошла ошибка'): string {
  const data = err?.response?.data;
  if (!data) return fallback;
  if (data.errors && typeof data.errors === 'object') {
    const msgs = Object.values(data.errors).flat() as string[];
    if (msgs.length) return msgs.join('\n');
  }
  if (typeof data === 'string') return data;
  if (data.error) return data.error;
  if (data.detail) return data.detail;
  if (data.title) return data.title;
  return fallback;
}

const statusMap: Record<number, { label: string; color: string; step: number }> = {
  0: { label: 'Новая',               color: 'cyan',       step: 0 },
  1: { label: 'Назначена',           color: 'blue',       step: 1 },
  2: { label: 'В работе',            color: 'processing', step: 2 },
  3: { label: 'Ожидание запчастей',  color: 'warning',    step: 2 },
  4: { label: 'Выполнена',           color: 'success',    step: 3 },
  5: { label: 'Закрыта',             color: 'default',    step: 4 },
  6: { label: 'Отменена',            color: 'error',      step: 4 },
  7: { label: 'Архив',               color: 'purple',     step: 4 },
};

const priorityMap: Record<number, { label: string; color: string }> = {
  0: { label: 'Критический', color: 'red' },
  1: { label: 'Высокий',     color: 'volcano' },
  2: { label: 'Обычный',     color: 'gold' },
  3: { label: 'Низкий',      color: 'green' },
};

const TicketDetailsPage: React.FC = () => {
  const { id } = useParams();
  const navigate = useNavigate();
  const [ticket, setTicket] = useState<any>(null);
  const [loading, setLoading] = useState(true);
  const [engineers, setEngineers] = useState([]);
  const [comment, setComment] = useState('');
  const [workActs, setWorkActs] = useState<any[]>([]);
  const [isActModalOpen, setIsActModalOpen] = useState(false);
  const [actForm] = Form.useForm();
  const [statusError, setStatusError] = useState<string | null>(null);
  const [uploadingActId, setUploadingActId] = useState<string | null>(null);

  const fetchDetails = async () => {
    try {
      const [ticketRes, engRes, actsRes] = await Promise.all([
        api.get(`/Tickets/${id}`),
        api.get('/Tickets/engineers'),
        api.get(`/WorkActs/ticket/${id}`),
      ]);
      setTicket(ticketRes.data);
      setEngineers(engRes.data);
      setWorkActs(actsRes.data);
    } catch {
      message.error('Не удалось загрузить данные заявки');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { fetchDetails(); }, [id]);

  const handleStatusChange = async (newStatus: number) => {
    setStatusError(null);
    if (newStatus === ticket.status) {
      setStatusError(`Заявка уже имеет статус «${statusMap[newStatus]?.label}».`);
      return;
    }
    if (ticket.status === 7) {
      setStatusError('Архивная заявка не может быть изменена.');
      return;
    }
    try {
      await api.put(`/Tickets/${id}/status`, {
        ticketId: id,
        newStatus,
        comment: 'Статус изменён вручную',
      });
      message.success(`Статус изменён на «${statusMap[newStatus]?.label}»`);
      fetchDetails();
    } catch (err: any) {
      setStatusError(parseApiErrors(err, 'Ошибка при обновлении статуса'));
    }
  };

  const handleAssign = async (assignedUserId: string) => {
    try {
      await api.put(`/Tickets/${id}/assign`, { ticketId: id, assignedUserId });
      message.success('Исполнитель назначен');
      fetchDetails();
    } catch (err: any) {
      message.error(parseApiErrors(err, 'Ошибка назначения'));
    }
  };

  const handleAddComment = async (isInternal: boolean = false) => {
    if (!comment.trim()) { message.warning('Введите текст комментария'); return; }
    try {
      await api.post(`/Tickets/${id}/comments`, { ticketId: id, text: comment, isInternal });
      setComment('');
      fetchDetails();
      message.success('Комментарий добавлен');
    } catch (err: any) {
      message.error(parseApiErrors(err, 'Не удалось добавить комментарий'));
    }
  };

  const handleAddWorkAct = async (values: any) => {
    try {
      await api.post('/WorkActs', {
        ...values,
        ticketId: id,
        workDate: values.workDate.toISOString(),
      });
      message.success('Акт создан');
      setIsActModalOpen(false);
      actForm.resetFields();
      fetchDetails();
    } catch (err: any) {
      message.error(parseApiErrors(err, 'Не удалось создать акт'));
    }
  };

  const handleFileUpload = async (actId: string, file: File) => {
    const allowedExts = ['.docx', '.xlsx', '.pdf', '.doc', '.xls'];
    const ext = '.' + file.name.split('.').pop()?.toLowerCase();
    if (!allowedExts.includes(ext)) {
      message.error('Разрешены файлы: docx, xlsx, pdf, doc, xls');
      return;
    }
    setUploadingActId(actId);
    try {
      const formData = new FormData();
      formData.append('file', file);

      const token = localStorage.getItem('accessToken');
      const res = await fetch(`/api/v1/WorkActs/${actId}/attachments`, {
        method: 'POST',
        headers: { Authorization: `Bearer ${token}` },

        body: formData,
      });

      if (!res.ok) {
        const text = await res.text().catch(() => '');
        throw new Error(text || `HTTP ${res.status}`);
      }

      message.success('Файл прикреплён');
      fetchDetails();
    } catch (err: any) {
      message.error(err?.message || 'Ошибка при загрузке файла');
    } finally {
      setUploadingActId(null);
    }
  };

  const handleDeleteAttachment = async (attachmentId: string) => {
    try {
      await api.delete(`/WorkActs/attachments/${attachmentId}`);
      message.success('Вложение удалено');
      fetchDetails();
    } catch {
      message.error('Не удалось удалить вложение');
    }
  };

  const downloadAttachment = (attachmentId: string, fileName: string) => {
    api.get(`/WorkActs/attachments/${attachmentId}/download`, { responseType: 'blob' }).then(res => {
      const url = window.URL.createObjectURL(new Blob([res.data]));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', fileName);
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(url);
    }).catch(() => message.error('Не удалось скачать файл'));
  };

  if (loading) return <div style={{ padding: 40, textAlign: 'center' }}>Загрузка...</div>;
  if (!ticket) return <div style={{ padding: 40 }}>Заявка не найдена</div>;

  const currentStatusInfo = statusMap[ticket.status] ?? statusMap[0];
  const isClosed   = ticket.status === 5;
  const isTerminal = ticket.status === 7 || ticket.status === 6;

  const statusBtn = (newStatus: number, label: string, btnType: 'default' | 'primary' | 'dashed' = 'default', danger = false) => {
    const same     = ticket.status === newStatus;
    const disabled = same || isTerminal;
    const btn = (
      <Button
        block
        type={btnType}
        danger={danger}
        disabled={disabled}
        onClick={() => handleStatusChange(newStatus)}
      >
        {label}
      </Button>
    );
    if (isTerminal) return <Tooltip title="Заявка находится в конечном состоянии">{btn}</Tooltip>;
    if (same) return <Tooltip title={`Заявка уже «${statusMap[newStatus]?.label}»`}>{btn}</Tooltip>;
    return btn;
  };

  return (
    <div style={{ maxWidth: 1200, margin: '0 auto', width: '100%' }}>
      <Row gutter={[24, 24]}>
        {}
        <Col span={24}>
          <Card>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', flexWrap: 'wrap', gap: 12 }}>
              <div style={{ minWidth: 0, flex: 1 }}>
                <Space align="center" wrap>
                  <Title level={3} style={{ margin: 0, wordBreak: 'break-word' }}>
                    Заявка #{ticket.id.substring(0, 8)}: {ticket.title}
                  </Title>
                  <Tag color={currentStatusInfo.color}>{currentStatusInfo.label}</Tag>
                  <Tag color={priorityMap[ticket.priority].color}>{priorityMap[ticket.priority].label}</Tag>
                </Space>
                <div style={{ marginTop: 8, color: '#888' }}>
                  Создана {dayjs(ticket.createdAt).format('LLL')} •{' '}
                  Исполнитель: {ticket.assignedUserName || 'Не назначен'}
                </div>
              </div>
              <Space wrap>
                <Button onClick={() => navigate('/tickets')}>К списку</Button>
                {ticket.status < 4 && (
                  <Button
                    type="primary"
                    icon={<CheckCircleOutlined />}
                    onClick={() => handleStatusChange(4)}
                  >
                    Отметить выполненной
                  </Button>
                )}
              </Space>
            </div>
            <Divider />
            <Steps
              current={currentStatusInfo.step}
              size="small"
              items={[
                { title: 'Новая' },
                { title: 'Назначена' },
                { title: 'В работе' },
                { title: 'Завершена' },
                { title: 'Закрыта' },
              ]}
            />
          </Card>
        </Col>

        {}
        <Col xs={24} md={16}>
          <Card title="Подробная информация" style={{ marginBottom: 24 }}>
            <Paragraph style={{ whiteSpace: 'pre-wrap', fontSize: 16 }}>{ticket.description}</Paragraph>
            <Descriptions bordered column={2} style={{ marginTop: 24 }}>
              <Descriptions.Item label="Клиент" span={2}>{ticket.clientName}</Descriptions.Item>
              <Descriptions.Item label="Объект">{ticket.serviceObjectName || 'Не указан'}</Descriptions.Item>
              <Descriptions.Item label="Оборудование">{ticket.equipmentName || 'Общий выезд'}</Descriptions.Item>
              <Descriptions.Item label="Тип">
                {ticket.type === 0 ? 'Аварийная' : ticket.type === 1 ? 'Плановая' : 'Консультация'}
              </Descriptions.Item>
              <Descriptions.Item label="Срок исполнения">
                {ticket.dueDate ? (
                  dayjs(ticket.dueDate).isBefore(dayjs()) && ticket.status < 4
                    ? <Text type="danger"><WarningOutlined /> {dayjs(ticket.dueDate).format('LL')} (просрочена)</Text>
                    : dayjs(ticket.dueDate).format('LL')
                ) : 'Не задан'}
              </Descriptions.Item>
            </Descriptions>
          </Card>

          <Card style={{ marginBottom: 24 }}>
            <Tabs defaultActiveKey="timeline" items={[
              {
                key: 'timeline',
                label: <Space><CommentOutlined /> Переписка</Space>,
                children: (
                  <>
                    <div style={{ marginBottom: 24 }}>
                      <Input.TextArea
                        rows={3}
                        placeholder="Напишите комментарий..."
                        value={comment}
                        onChange={(e) => setComment(e.target.value)}
                      />
                      <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: 8 }}>
                        <Space>
                          <Button type="link" onClick={() => handleAddComment(true)}>Внутренняя заметка</Button>
                          <Button type="primary" icon={<CommentOutlined />} onClick={() => handleAddComment(false)}>
                            Отправить публично
                          </Button>
                        </Space>
                      </div>
                    </div>
                    <Timeline mode="left">
                      {ticket.comments.map((c: any) => (
                        <Timeline.Item key={c.id} color="blue" label={dayjs(c.createdAt).format('DD MMM HH:mm')}>
                          <Card size="small" style={{ backgroundColor: c.isInternal ? '#fffbe6' : 'inherit' }}>
                            <Text strong>{c.isInternal ? '[ВНУТРЕННЯЯ] Сотрудник' : 'Пользователь системы'}</Text>
                            <p style={{ margin: '8px 0 0' }}>{c.text}</p>
                          </Card>
                        </Timeline.Item>
                      ))}
                      {ticket.history.map((h: any) => (
                        <Timeline.Item key={h.id} color="gray" label={dayjs(h.changedAt).format('DD MMM HH:mm')}>
                          <Text italic>
                            Статус: <b>{statusMap[h.oldStatus]?.label}</b> → <b>{statusMap[h.newStatus]?.label}</b>
                          </Text>
                          {h.comment && <p style={{ fontSize: 12, color: '#888' }}>Причина: {h.comment}</p>}
                        </Timeline.Item>
                      ))}
                    </Timeline>
                  </>
                ),
              },
              {
                key: 'workActs',
                label: <Space><FileDoneOutlined /> Акты работ</Space>,
                children: (
                  <>
                    <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: 16 }}>
                      <Button type="primary" icon={<PlusOutlined />} onClick={() => setIsActModalOpen(true)} disabled={isTerminal}>
                        Добавить акт
                      </Button>
                    </div>
                    <List
                      dataSource={workActs}
                      locale={{ emptyText: 'Акты работ отсутствуют' }}
                      renderItem={(act: any) => (
                        <List.Item style={{ padding: 0, marginBottom: 12 }}>
                          <Card size="small" style={{ width: '100%' }}>
                            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                              <Space wrap>
                                <Text strong>{act.number}</Text>
                                <Tag>{dayjs(act.workDate).format('LL')}</Tag>
                                {act.invoiceId && <Tag color="green">Счёт выставлен</Tag>}
                              </Space>
                              <Text strong>{act.totalCost.toLocaleString()} ₸</Text>
                            </div>
                            <p style={{ margin: '8px 0', color: '#666' }}>{act.description}</p>
                            <div style={{ fontSize: 12, color: '#999', marginBottom: 8 }}>
                              Работы: {act.laborCost.toLocaleString()} ₸ | Материалы: {act.materialsCost.toLocaleString()} ₸
                            </div>
                            {}
                            {act.attachments && act.attachments.length > 0 && (
                              <div style={{ marginBottom: 8 }}>
                                {act.attachments.map((att: any) => (
                                  <div key={att.id} style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 4 }}>
                                    <PaperClipOutlined style={{ color: '#1890ff' }} />
                                    <Text style={{ fontSize: 12 }}>{att.fileName}</Text>
                                    <Text style={{ fontSize: 11, color: '#999' }}>
                                      ({(att.fileSize / 1024).toFixed(0)} КБ)
                                    </Text>
                                    <Button
                                      size="small"
                                      type="link"
                                      icon={<DownloadOutlined />}
                                      onClick={() => downloadAttachment(att.id, att.fileName)}
                                    />
                                    <Button
                                      size="small"
                                      type="link"
                                      danger
                                      icon={<DeleteOutlined />}
                                      onClick={() => handleDeleteAttachment(att.id)}
                                    />
                                  </div>
                                ))}
                              </div>
                            )}
                            {}
                            <div>
                              <input
                                type="file"
                                accept=".docx,.xlsx,.pdf,.doc,.xls"
                                style={{ display: 'none' }}
                                id={`file-input-${act.id}`}
                                onChange={(e) => {
                                  const file = e.target.files?.[0];
                                  if (file) handleFileUpload(act.id, file);
                                  e.target.value = '';
                                }}
                              />
                              <Button
                                size="small"
                                icon={<PaperClipOutlined />}
                                loading={uploadingActId === act.id}
                                onClick={() => document.getElementById(`file-input-${act.id}`)?.click()}
                              >
                                Прикрепить файл
                              </Button>
                            </div>
                          </Card>
                        </List.Item>
                      )}
                    />
                  </>
                ),
              },
            ]} />
          </Card>
        </Col>

        {}
        <Col xs={24} md={8}>
          <Card title="Назначение и управление" style={{ marginBottom: 24 }}>
            <div style={{ marginBottom: 16 }}>
              <label style={{ display: 'block', marginBottom: 8, color: '#888' }}>Исполнитель</label>
              <Select
                style={{ width: '100%' }}
                placeholder="Назначить исполнителя"
                value={ticket.assignedUserId}
                onChange={handleAssign}
                disabled={isTerminal}
              >
                {engineers.map((e: any) => (
                  <Select.Option key={e.id} value={e.id}>{e.fullName}</Select.Option>
                ))}
              </Select>
            </div>

            <Divider />

            {statusError && (
              <Alert
                type="warning"
                showIcon
                closable
                message={statusError}
                style={{ marginBottom: 12, whiteSpace: 'pre-line' }}
                onClose={() => setStatusError(null)}
              />
            )}

            <Space direction="vertical" style={{ width: '100%' }}>
              {statusBtn(2, 'Взять в работу')}
              {statusBtn(3, 'Ожидание запчастей')}
              <Divider style={{ margin: '4px 0' }} />
              {statusBtn(5, 'Закрыть заявку', 'default', true)}

              {}
              {isClosed && (
                <Tooltip title="Переоткрыть — вернуть заявку в работу">
                  <Button
                    block
                    icon={<ReloadOutlined />}
                    onClick={() => handleStatusChange(2)}
                  >
                    Переоткрыть заявку
                  </Button>
                </Tooltip>
              )}

              {}
              {(ticket.status === 4 || ticket.status === 5) && (
                <Tooltip title="Перенести в архив — заявка будет скрыта из основного списка">
                  <Button
                    block
                    icon={<InboxOutlined />}
                    onClick={() => handleStatusChange(7)}
                  >
                    Отправить в архив
                  </Button>
                </Tooltip>
              )}

              {}
              {ticket.status === 7 && (
                <Alert type="info" message="Заявка находится в архиве" showIcon />
              )}
            </Space>
          </Card>

          <Card title="Краткая информация">
            <Badge.Ribbon
              text={priorityMap[ticket.priority].label}
              color={priorityMap[ticket.priority].color}
            >
              <div style={{ padding: '10px 0' }}>
                <p><ClockCircleOutlined /> Плановое время: ~4ч</p>
                <p><UserOutlined /> Источник: Портал</p>
              </div>
            </Badge.Ribbon>
          </Card>
        </Col>
      </Row>

      {}
      <Modal
        title="Добавить акт выполненных работ"
        open={isActModalOpen}
        onCancel={() => { setIsActModalOpen(false); actForm.resetFields(); }}
        onOk={() => actForm.submit()}
        okText="Создать"
        cancelText="Отмена"
      >
        <Form form={actForm} layout="vertical" onFinish={handleAddWorkAct}>
          <Form.Item name="number" label="Номер акта" rules={[{ required: true, message: 'Введите номер акта' }]}>
            <Input placeholder="Например: АКТ-001" />
          </Form.Item>
          <Form.Item
            name="workDate"
            label="Дата выполнения работ"
            rules={[{ required: true, message: 'Укажите дату' }]}
            initialValue={dayjs()}
          >
            <DatePicker style={{ width: '100%' }} />
          </Form.Item>
          <Form.Item
            name="description"
            label="Описание работ"
            rules={[{ required: true, message: 'Опишите выполненные работы' }]}
          >
            <Input.TextArea rows={3} placeholder="Что было сделано?" />
          </Form.Item>
          <Row gutter={16}>
            <Col span={12}>
              <Form.Item name="laborCost" label="Стоимость работ (₸)" rules={[{ required: true }]} initialValue={0}>
                <InputNumber style={{ width: '100%' }} min={0} />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="materialsCost" label="Стоимость материалов (₸)" rules={[{ required: true }]} initialValue={0}>
                <InputNumber style={{ width: '100%' }} min={0} />
              </Form.Item>
            </Col>
          </Row>
        </Form>
      </Modal>
    </div>
  );
};

export default TicketDetailsPage;
